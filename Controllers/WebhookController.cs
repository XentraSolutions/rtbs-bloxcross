using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Data;
using Rtbs.Bloxcross.Models;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace Rtbs.Bloxcross.Controllers;

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
        if (!Request.Headers.TryGetValue("CLIENT_ID", out var clientId) ||
            !Request.Headers.TryGetValue("X-API-KEY", out var apiKey) ||
            !Request.Headers.TryGetValue("X-TIMESTAMP", out var timestampHeader) ||
            !Request.Headers.TryGetValue("X-SIGNATURE", out var signatureHeader))
        {
            return BadRequest(new { error = "Missing required webhook headers." });
        }

        if (!long.TryParse(timestampHeader.ToString(), out var timestampMs))
        {
            return BadRequest(new { error = "Invalid X-TIMESTAMP format." });
        }

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
        var now = DateTimeOffset.UtcNow;
        if (Math.Abs((now - timestamp).TotalMinutes) > 5)
        {
            return Unauthorized(new { error = "Timestamp is outside the allowed window." });
        }

        var urlPath = Request.Path + Request.QueryString;
        var message = $"{timestampHeader}POST{urlPath}";

        var secretConfig = _config["Webhook:SecretKey"]
                           ?? _config["BLOXCROSS:SecretKey"]
                           ?? _config["SECRET_KEY"]
                           ?? _config["WebhookSecret"];

        if (string.IsNullOrEmpty(secretConfig))
        {
            return StatusCode(500, new { error = "Webhook secret is not configured." });
        }

        var key = GetKeyBytes(secretConfig);

        byte[] expectedHash;
        using (var hmac = new HMACSHA256(key))
        {
            expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        }

        byte[] providedSig;
        try
        {
            providedSig = ParseSignature(signatureHeader.ToString());
        }
        catch
        {
            return Unauthorized(new { error = "Invalid signature format." });
        }

        if (providedSig == null || providedSig.Length != expectedHash.Length ||
            !CryptographicOperations.FixedTimeEquals(providedSig, expectedHash))
        {
            return Unauthorized(new { error = "Invalid signature." });
        }

        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest(new { error = "Empty request body." });
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ivi", out var ivElem) || !root.TryGetProperty("payload", out var payloadElem))
            {
                return BadRequest(new { error = "Missing iv or payload in body." });
            }

            var ivB64 = ivElem.GetString();
            var payloadB64 = payloadElem.GetString();

            if (string.IsNullOrEmpty(ivB64) || string.IsNullOrEmpty(payloadB64))
            {
                return BadRequest(new { error = "iv or payload is empty." });
            }

            var iv = Convert.FromBase64String(ivB64);
            var ciphertextWithTag = Convert.FromBase64String(payloadB64);

            const int TagLength = 16;
            if (ciphertextWithTag.Length < TagLength)
            {
                return BadRequest(new { error = "Payload is too short to contain a GCM tag." });
            }

            var tag = ciphertextWithTag[^TagLength..];
            var ciphertext = ciphertextWithTag[..^TagLength];
            var plaintext = new byte[ciphertext.Length];

            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                using var sha = SHA256.Create();
                key = sha.ComputeHash(key);
            }

            using var aes = new AesGcm(key);
            aes.Decrypt(iv, ciphertext, tag, plaintext);

            var decrypted = Encoding.UTF8.GetString(plaintext);

            return Ok(new { success = true });
        }
        catch (CryptographicException)
        {
            return Unauthorized(new { error = "Decryption failed (authentication tag mismatch)." });
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "Invalid JSON payload." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }


    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] WebhookEvent @event)
    {
        if (@event is null)
            return BadRequest(new { error = "Missing or invalid body." });

        var log = new WebhookLog
        {
            EventType = @event.EventType,
            TransactionId = @event.TransactionId.ToString(),
            PortfolioId = @event.PortfolioId,
            Amount = @event.Quantity,
            Currency = @event.BaseCurrency,
            Status = @event.Status,
            RawPayload = JsonSerializer.Serialize(@event),
            ReceivedAt = DateTime.UtcNow
        };

        _context.WebhookLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = log.Id });
    }

    #region Helpers
    private static byte[] ParseSignature(string signature)
    {
        try
        {
            return Convert.FromBase64String(signature);
        }
        catch { }

        if (IsHexString(signature))
        {
            return HexToBytes(signature);
        }

        throw new FormatException("Unsupported signature encoding.");
    }

    private static bool IsHexString(string input)
    {
        if (string.IsNullOrEmpty(input) || (input.Length % 2) != 0) return false;
        foreach (var c in input)
        {
            var isHex = (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }

    private static byte[] HexToBytes(string hex)
    {
        var len = hex.Length / 2;
        var bytes = new byte[len];
        for (int i = 0; i < len; i++)
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
            if (b.Length > 0) return b;
        }
        catch { }

        if (IsHexString(secret))
        {
            try { return HexToBytes(secret); } catch { }
        }

        var utf = Encoding.UTF8.GetBytes(secret);
        if (utf.Length == 16 || utf.Length == 24 || utf.Length == 32) return utf;

        using var sha = SHA256.Create();
        return sha.ComputeHash(utf);
    }
    #endregion
}