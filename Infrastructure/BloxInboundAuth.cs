using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

public sealed class BloxInboundAuthFilter : IAsyncAuthorizationFilter
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);
    private readonly IBloxCredentialRepository _credentialRepository;
    private readonly ILogger<BloxInboundAuthFilter> _logger;

    public BloxInboundAuthFilter(
        IBloxCredentialRepository credentialRepository,
        ILogger<BloxInboundAuthFilter> logger)
    {
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (ShouldSkip(context))
        {
            return;
        }

        var request = context.HttpContext.Request;

        try
        {
            if (!TryGetRequiredHeader(request.Headers, BloxInboundAuthSpec.ApiKeyHeader, out var apiKey) ||
                !TryGetRequiredHeader(request.Headers, BloxInboundAuthSpec.ClientIdHeader, out var clientId) ||
                !TryGetRequiredHeader(request.Headers, BloxInboundAuthSpec.TimestampHeader, out var timestamp) ||
                !TryGetRequiredHeader(request.Headers, BloxInboundAuthSpec.SignatureHeader, out var signature))
            {
                context.Result = Error(401, BloxInboundAuthErrors.IncompleteSecurityHeader);
                return;
            }

            if (!TryParseTimestamp(timestamp, out var requestTimestamp) ||
                !IsTimestampWithinAllowedWindow(requestTimestamp))
            {
                context.Result = Error(401, BloxInboundAuthErrors.InvalidSecurityHeader);
                return;
            }

            var credential = await _credentialRepository.GetActiveAsync();
            if (string.IsNullOrWhiteSpace(credential.ClientId) ||
                string.IsNullOrWhiteSpace(credential.ApiKey) ||
                string.IsNullOrWhiteSpace(credential.SecretKey))
            {
                context.Result = Error(401, BloxInboundAuthErrors.ErrorInSecurity);
                return;
            }

            if (!string.Equals(apiKey, credential.ApiKey, StringComparison.Ordinal) ||
                !string.Equals(clientId, credential.ClientId, StringComparison.Ordinal))
            {
                context.Result = Error(401, BloxInboundAuthErrors.InvalidSecurityHeader);
                return;
            }

            byte[] providedSignature;
            try
            {
                providedSignature = BloxHmacHelper.ParseSignature(signature);
            }
            catch (FormatException)
            {
                context.Result = Error(401, BloxInboundAuthErrors.InvalidSignature);
                return;
            }

            var signaturePath = BloxHmacHelper.GetPathForSignature($"{request.PathBase}{request.Path}");
            var expectedSignature = BloxHmacHelper.ComputeSignatureBytes(
                credential.SecretKey,
                timestamp,
                request.Method,
                signaturePath);

            if (providedSignature.Length != expectedSignature.Length ||
                !CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
            {
                context.Result = Error(401, BloxInboundAuthErrors.InvalidSignature);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inbound HMAC authentication failed unexpectedly.");
            context.Result = Error(401, BloxInboundAuthErrors.ErrorInSecurity);
        }
    }

    private static bool ShouldSkip(AuthorizationFilterContext context)
    {
        return context.ActionDescriptor.EndpointMetadata
            .OfType<SkipBloxInboundAuthAttribute>()
            .Any();
    }

    private static bool TryGetRequiredHeader(IHeaderDictionary headers, string name, out string value)
    {
        value = string.Empty;

        if (!headers.TryGetValue(name, out var headerValue))
        {
            return false;
        }

        value = headerValue.ToString().Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseTimestamp(string timestamp, out DateTimeOffset requestTimestamp)
    {
        requestTimestamp = default;

        if (!long.TryParse(timestamp, out var timestampMs))
        {
            return false;
        }

        try
        {
            requestTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool IsTimestampWithinAllowedWindow(DateTimeOffset requestTimestamp)
    {
        var timeDifference = DateTimeOffset.UtcNow - requestTimestamp;
        return timeDifference <= AllowedClockSkew && timeDifference >= -AllowedClockSkew;
    }

    private static ObjectResult Error(int statusCode, string message)
    {
        return ApiResponseFactory.Failure(statusCode, message);
    }
}

public static class BloxInboundAuthErrors
{
    public const string IncompleteSecurityHeader = "Incomplete security header";
    public const string InvalidSecurityHeader = "Invalid security header";
    public const string InvalidSignature = "Invalid signature";
    public const string ErrorInSecurity = "Error in security";
}

public static class BloxInboundAuthAutoFillMiddleware
{
    public static bool ShouldAutoFill(HttpRequest request)
    {
        var path = request.Path;
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
               !path.StartsWithSegments("/api/Webhook", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task FillMissingHeadersAsync(HttpRequest request, IBloxCredentialRepository repository)
    {
        if (HasAllRequiredHeaders(request.Headers))
        {
            return;
        }

        var credential = await repository.GetActiveAsync();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signaturePath = BloxHmacHelper.GetPathForSignature($"{request.PathBase}{request.Path}");
        var signature = BloxHmacHelper.ComputeSignatureBase64(
            credential.SecretKey,
            timestamp,
            request.Method,
            signaturePath);

        request.Headers[BloxInboundAuthSpec.ApiKeyHeader] = credential.ApiKey;
        request.Headers[BloxInboundAuthSpec.ClientIdHeader] = credential.ClientId;
        request.Headers[BloxInboundAuthSpec.TimestampHeader] = timestamp;
        request.Headers[BloxInboundAuthSpec.SignatureHeader] = signature;
    }

    private static bool HasAllRequiredHeaders(IHeaderDictionary headers)
    {
        return HasHeader(headers, BloxInboundAuthSpec.ApiKeyHeader) &&
               HasHeader(headers, BloxInboundAuthSpec.ClientIdHeader) &&
               HasHeader(headers, BloxInboundAuthSpec.TimestampHeader) &&
               HasHeader(headers, BloxInboundAuthSpec.SignatureHeader);
    }

    private static bool HasHeader(IHeaderDictionary headers, string name)
    {
        return headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value.ToString());
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SkipBloxInboundAuthAttribute : Attribute
{
}

public static class BloxInboundAuthSpec
{
    public const string ApiKeyHeader = "X-API-KEY";
    public const string ClientIdHeader = "CLIENT_ID";
    public const string TimestampHeader = "X-TIMESTAMP";
    public const string SignatureHeader = "X-SIGNATURE";
}

public static class BloxHmacHelper
{
    public static string GetPathForSignature(string path)
    {
        var queryIndex = path.IndexOf('?');
        return queryIndex >= 0 ? path[..queryIndex] : path;
    }

    public static byte[] ComputeSignatureBytes(string secretKey, string timestamp, string method, string path)
    {
        var message = $"{timestamp}{method.ToUpperInvariant()}{path}";
        var keyBytes = GetKeyBytes(secretKey);

        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    public static string ComputeSignatureBase64(string secretKey, string timestamp, string method, string path)
    {
        return Convert.ToBase64String(ComputeSignatureBytes(secretKey, timestamp, method, path));
    }

    public static byte[] ParseSignature(string signature)
    {
        try
        {
            return Convert.FromBase64String(signature);
        }
        catch (FormatException)
        {
        }

        if (IsHexString(signature))
        {
            return HexToBytes(signature);
        }

        throw new FormatException("Unsupported signature encoding.");
    }

    public static byte[] GetKeyBytes(string secret)
    {
        var normalized = secret.Trim();

        try
        {
            var decoded = Convert.FromBase64String(normalized);
            if (decoded.Length > 0)
            {
                return decoded;
            }
        }
        catch (FormatException)
        {
        }

        if (IsHexString(normalized))
        {
            return HexToBytes(normalized);
        }

        return Encoding.UTF8.GetBytes(normalized);
    }

    private static bool IsHexString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length % 2 != 0)
        {
            return false;
        }

        foreach (var character in input)
        {
            var isHexCharacter = (character >= '0' && character <= '9') ||
                                 (character >= 'a' && character <= 'f') ||
                                 (character >= 'A' && character <= 'F');
            if (!isHexCharacter)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }
}
