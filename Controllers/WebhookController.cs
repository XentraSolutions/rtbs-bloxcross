using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Data;
using Rtbs.Bloxcross.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;

    public WebhookController(IConfiguration config, AppDbContext context)
    {
        _config = config;
        _context = context;
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        if (!TryGetWebhookHeaders(out var timestampHeader, out var signatureHeader))
        {
            return BadRequest(new { error = "Missing required webhook headers." });
        }

        if (!long.TryParse(timestampHeader, out _))
        {
            return BadRequest(new { error = "Invalid X-TIMESTAMP format." });
        }

        if (!IsTimestampWithinAllowedWindow(timestampHeader))
        {
            return Unauthorized(new { error = "Timestamp is outside the allowed window." });
        }

        var secretConfig = GetSecretKey();
        if (string.IsNullOrWhiteSpace(secretConfig))
        {
            return StatusCode(500, new { error = "Webhook secret is not configured." });
        }

        var key = GetKeyBytes(secretConfig);
        var body = await ReadRequestBodyAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest(new { error = "Empty request body." });
        }

        if (!IsSignatureValid(timestampHeader, signatureHeader, key))
        {
            return Unauthorized(new { error = "Invalid signature." });
        }

        try
        {
            var encrypted = JsonSerializer.Deserialize<WebhookEncryptedData>(body);
            if (encrypted is null || string.IsNullOrWhiteSpace(encrypted.Iv) || string.IsNullOrWhiteSpace(encrypted.Payload))
            {
                return BadRequest(new { error = "Missing iv or payload in body." });
            }

            var decryptedPayload = DecryptPayload(encrypted.Iv, encrypted.Payload, key);
            var webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(decryptedPayload);
            if (webhookEvent is null)
            {
                return BadRequest(new { error = "Invalid decrypted webhook payload." });
            }

            var log = new WebhookLog
            {
                EventType = webhookEvent.EventType,
                TransactionId = webhookEvent.TransactionId.ToString(),
                PortfolioId = webhookEvent.PortfolioId,
                Amount = webhookEvent.Quantity,
                Currency = webhookEvent.BaseCurrency,
                Status = webhookEvent.Status,
                RawPayload = decryptedPayload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.WebhookLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = log.Id });
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid base64 payload format." });
        }
        catch (CryptographicException)
        {
            return Unauthorized(new { error = "Decryption failed (authentication tag mismatch)." });
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Invalid JSON payload." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private bool TryGetWebhookHeaders(out string timestampHeader, out string signatureHeader)
    {
        timestampHeader = string.Empty;
        signatureHeader = string.Empty;

        if (!Request.Headers.TryGetValue("CLIENT_ID", out _) ||
            !Request.Headers.TryGetValue("X-API-KEY", out _) ||
            !Request.Headers.TryGetValue("X-TIMESTAMP", out var timestampValue) ||
            !Request.Headers.TryGetValue("X-SIGNATURE", out var signatureValue))
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

    private string? GetSecretKey()
    {
        return _config["Webhook:SecretKey"]
               ?? _config["BLOXCROSS:SecretKey"]
               ?? _config["SECRET_KEY"]
               ?? _config["WebhookSecret"];
    }

    private async Task<string> ReadRequestBodyAsync()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private bool IsSignatureValid(string timestampHeader, string signatureHeader, byte[] key)
    {
        var message = $"{timestampHeader}POST{Request.Path}{Request.QueryString}";
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
        try
        {
            var b = Convert.FromBase64String(secret);
            if (b.Length > 0)
            {
                return b;
            }
        }
        catch
        {
        }

        if (IsHexString(secret))
        {
            try
            {
                return HexToBytes(secret);
            }
            catch
            {
            }
        }

        var utf = Encoding.UTF8.GetBytes(secret);
        return utf;
    }
}
