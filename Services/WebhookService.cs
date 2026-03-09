using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rtbs.Bloxcross.Data;
using Rtbs.Bloxcross.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class WebhookService : IWebhookService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebhookService(IConfiguration config, AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WebhookValidationResult> ValidateAndProcessWebhookAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "HTTP context not available",
                StatusCode = 500
            };
        }

        if (!TryGetWebhookHeaders(context, out var timestampHeader, out var signatureHeader))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Missing required webhook headers.",
                StatusCode = 400
            };
        }

        if (!long.TryParse(timestampHeader, out _))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid X-TIMESTAMP format.",
                StatusCode = 400
            };
        }

        if (!IsTimestampWithinAllowedWindow(timestampHeader))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Timestamp is outside the allowed window.",
                StatusCode = 401
            };
        }

        var signatureSecretConfig = GetSignatureSecretKey();
        if (string.IsNullOrWhiteSpace(signatureSecretConfig))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Webhook signature secret is not configured.",
                StatusCode = 500
            };
        }

        var decryptSecretConfig = GetDecryptSecretKey(signatureSecretConfig);
        if (string.IsNullOrWhiteSpace(decryptSecretConfig))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Webhook decrypt secret is not configured.",
                StatusCode = 500
            };
        }

        var signatureKey = GetKeyBytes(signatureSecretConfig);
        var decryptKey = GetKeyBytes(decryptSecretConfig);
        var body = await ReadRequestBodyAsync(context);
        if (string.IsNullOrWhiteSpace(body))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Empty request body.",
                StatusCode = 400
            };
        }

        if (!IsSignatureValid(context, timestampHeader, signatureHeader, signatureKey))
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid signature.",
                StatusCode = 401
            };
        }

        try
        {
            var encrypted = JsonSerializer.Deserialize<WebhookEncryptedData>(body);
            if (encrypted is null || string.IsNullOrWhiteSpace(encrypted.Iv) || string.IsNullOrWhiteSpace(encrypted.Payload))
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Missing iv or payload in body.",
                    StatusCode = 400
                };
            }

            var decryptedPayload = DecryptPayload(encrypted.Iv, encrypted.Payload, decryptKey);
            var webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(decryptedPayload);
            if (webhookEvent is null)
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid decrypted webhook payload.",
                    StatusCode = 400
                };
            }

            var transactionId = webhookEvent.TransactionId.ToString();
            var duplicateExists = await _context.WebhookLogs.AnyAsync(x =>
                x.TransactionId == transactionId &&
                x.EventType == webhookEvent.EventType &&
                x.Status == webhookEvent.Status);
            if (duplicateExists)
            {
                return new WebhookValidationResult
                {
                    IsValid = true,
                    IsDuplicate = true,
                    StatusCode = 200
                };
            }

            var log = new WebhookLog
            {
                EventType = webhookEvent.EventType,
                TransactionId = transactionId,
                PortfolioId = webhookEvent.PortfolioId,
                Amount = webhookEvent.Quantity,
                Currency = webhookEvent.BaseCurrency,
                Status = webhookEvent.Status,
                RawPayload = decryptedPayload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.WebhookLogs.Add(log);
            await _context.SaveChangesAsync();

            return new WebhookValidationResult
            {
                IsValid = true,
                Log = log,
                StatusCode = 200
            };
        }
        catch (FormatException)
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid base64 payload format.",
                StatusCode = 400
            };
        }
        catch (CryptographicException)
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Decryption failed (authentication tag mismatch).",
                StatusCode = 401
            };
        }
        catch (JsonException)
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid JSON payload.",
                StatusCode = 400
            };
        }
        catch (InvalidOperationException ex)
        {
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
                StatusCode = 500
            };
        }
    }

    private bool TryGetWebhookHeaders(HttpContext context, out string timestampHeader, out string signatureHeader)
    {
        timestampHeader = string.Empty;
        signatureHeader = string.Empty;

        if (!context.Request.Headers.TryGetValue("X-TIMESTAMP", out var timestampValue) ||
            !context.Request.Headers.TryGetValue("X-SIGNATURE", out var signatureValue))
        {
            return false;
        }

        timestampHeader = timestampValue.ToString();
        signatureHeader = signatureValue.ToString();
        return true;
    }

    private static bool IsTimestampWithinAllowedWindow(string timestampHeader)
    {
        _ = long.TryParse(timestampHeader, out var timestampMs);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
        return Math.Abs((DateTimeOffset.UtcNow - timestamp).TotalMinutes) <= 5;
    }

    private string? GetSignatureSecretKey()
    {
        return _config["Webhook:SecretKey"]
               ?? _config["BLOXCROSS:SecretKey"]
               ?? _config["SECRET_KEY"]
               ?? _config["WebhookSecret"];
    }

    private string? GetDecryptSecretKey(string? fallbackSignatureSecret)
    {
        return _config["Webhook:DecryptSecretKey"]
               ?? _config["BLOXCROSS:DecryptSecretKey"]
               ?? _config["SECRET_KEY_DECRYPT"]
               ?? _config["WebhookDecryptSecret"]
               ?? fallbackSignatureSecret;
    }

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private bool IsSignatureValid(HttpContext context, string timestampHeader, string signatureHeader, byte[] key)
    {
        var message = $"{timestampHeader}{context.Request.Method.ToUpperInvariant()}{context.Request.Path}";
        byte[] expectedHash;
        using (var hmac = new HMACSHA256(key))
        {
            expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        }

        byte[] providedSig;
        try
        {
            providedSig = ParseSignature(signatureHeader);
        }
        catch
        {
            return false;
        }

        return providedSig.Length == expectedHash.Length &&
               CryptographicOperations.FixedTimeEquals(providedSig, expectedHash);
    }

    private static string DecryptPayload(string ivB64, string payloadB64, byte[] key)
    {
        var iv = Convert.FromBase64String(ivB64);
        var ciphertextWithTag = Convert.FromBase64String(payloadB64);

        const int tagLength = 16;
        if (ciphertextWithTag.Length < tagLength)
        {
            throw new FormatException("Payload is too short to contain a GCM tag.");
        }

        var tag = ciphertextWithTag[^tagLength..];
        var ciphertext = ciphertextWithTag[..^tagLength];
        var plaintext = new byte[ciphertext.Length];

        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
        {
            throw new InvalidOperationException("Webhook secret key length must be 16, 24, or 32 bytes for AES-GCM decryption.");
        }

        using var aes = new AesGcm(key, tagLength);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] ParseSignature(string signature)
    {
        try
        {
            return Convert.FromBase64String(signature);
        }
        catch
        {
        }

        if (IsHexString(signature))
        {
            return HexToBytes(signature);
        }

        throw new FormatException("Unsupported signature encoding.");
    }

    private static bool IsHexString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length % 2 != 0)
        {
            return false;
        }

        foreach (var c in input)
        {
            var isHex = (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F');
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] HexToBytes(string hex)
    {
        var len = hex.Length / 2;
        var bytes = new byte[len];
        for (var i = 0; i < len; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    private static byte[] GetKeyBytes(string secret)
    {
        var normalized = secret.Trim();
        try
        {
            var b = Convert.FromBase64String(normalized);
            if (b.Length > 0)
            {
                return b;
            }
        }
        catch
        {
        }

        if (IsHexString(normalized))
        {
            try
            {
                return HexToBytes(normalized);
            }
            catch
            {
            }
        }

        var utf = Encoding.UTF8.GetBytes(normalized);
        return utf;
    }
}
