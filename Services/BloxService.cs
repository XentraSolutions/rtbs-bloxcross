using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class BloxService : IBloxService
{
    private readonly HttpClient _httpClient;
    private readonly IBloxCredentialRepository _repository;
    private readonly IApiLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions SnakeCaseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    public BloxService(HttpClient httpClient,
                       IBloxCredentialRepository repository, IApiLogger logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _repository = repository;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private string GenerateSignature(string apiKey, string timestamp, string method, string path)
    {
        var message = $"{timestamp}{method.ToUpper()}{path}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));

        return Convert.ToHexString(hash).ToLower();
    }

    private async Task AddHeaders(HttpRequestMessage request, string method, string path)
    {
        var credential = await _repository.GetActiveAsync();

        _httpClient.BaseAddress = new Uri(credential.BaseUrl);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var signature = GenerateSignature(credential.ApiKey, timestamp, method, path);

        request.Headers.Add("CLIENT_ID", credential.ClientId);
        request.Headers.Add("X-API-KEY", credential.ApiKey);
        request.Headers.Add("X-TIMESTAMP", timestamp);
        request.Headers.Add("X-SIGNATURE", signature);
    }
    private string GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context != null)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim(); // first IP = real client
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
                return remoteIp;
        }

        return Dns.GetHostEntry(Dns.GetHostName())
                  .AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                  .ToString() ?? "Unknown";
    }
    public async Task<(bool Success, int StatusCode, string? Response, string? ErrorMessage)> GetAsync(string path)
    {
        string? content = null;
        string? error = null;
        var traceId = Guid.NewGuid().ToString();
        string ipAddress = GetIpAddress();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            await AddHeaders(request, "GET", path);

            var response = await _httpClient.SendAsync(request);
            content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                error = $"Status: {response.StatusCode}, Response: {content}";
                return (false, (int)response.StatusCode, content, error);
            }

            return (true, (int)response.StatusCode, content, null);
        }
        catch (HttpRequestException httpEx)
        {
            error = $"HttpRequestException: {httpEx.Message}";
            return (false, 502, null, error);
        }
        catch (Exception ex)
        {
            error = $"Exception: {ex.Message}";
            return (false, 500, null, error);
        }
        finally
        {
            var logModel = new ApiLogModel
            {
                MethodName = "GET " + path,
                Parameters = null,  // GET has no body
                Response = string.IsNullOrWhiteSpace(content) ? error : content,
                IpAddress = ipAddress,
                TraceId = traceId
            };

            await _logger.LogAsync(logModel);
        }
    }

    public async Task<(bool Success, int StatusCode, string? Response, string? ErrorMessage)> PostAsync(string path, object body)
    {
        string? content = null;
        string? error = null;
        var traceId = Guid.NewGuid().ToString();
        string ipAddress = GetIpAddress();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            await AddHeaders(request, "POST", path);

            request.Content = new StringContent(
                JsonSerializer.Serialize(body, SnakeCaseJson),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                error = $"Status: {response.StatusCode}, Response: {content}";
                return (false, (int)response.StatusCode, content, error);
            }

            return (true, (int)response.StatusCode, content, null);
        }
        catch (HttpRequestException httpEx)
        {
            error = $"HttpRequestException: {httpEx.Message}";
            return (false, 502, null, error);
        }
        catch (Exception ex)
        {
            error = $"Exception: {ex.Message}";
            return (false, 500, null, error);
        }
        finally
        {
            var logModel = new ApiLogModel
            {
                MethodName = "POST " + path,
                Parameters = body,
                Response = string.IsNullOrWhiteSpace(content) ? error : content,
                IpAddress = ipAddress,
                TraceId = traceId
            };

            await _logger.LogAsync(logModel);
        }
    }

    public async Task<(bool Success, int StatusCode, string? Response, string? ErrorMessage)> DeleteAsync(string path)
    {
        string? content = null;
        string? error = null;
        var traceId = Guid.NewGuid().ToString();
        string ipAddress = GetIpAddress();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, path);
            await AddHeaders(request, "DELETE", path);

            var response = await _httpClient.SendAsync(request);
            content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                error = $"Status: {response.StatusCode}, Response: {content}";
                return (false, (int)response.StatusCode, content, error);
            }

            return (true, (int)response.StatusCode, content, null);
        }
        catch (HttpRequestException httpEx)
        {
            error = $"HttpRequestException: {httpEx.Message}";
            return (false, 502, null, error);
        }
        catch (Exception ex)
        {
            error = $"Exception: {ex.Message}";
            return (false, 500, null, error);
        }
        finally
        {
            var logModel = new ApiLogModel
            {
                MethodName = "DELETE " + path,
                Parameters = null,
                Response = string.IsNullOrWhiteSpace(content) ? error : content,
                IpAddress = ipAddress,
                TraceId = traceId
            };

            await _logger.LogAsync(logModel);
        }
    }
}
