using Rtbs.Bloxcross.Data;
using System.Text.Json;
using System.Collections.Generic;

public class ApiLogger : IApiLogger
{
    private static readonly JsonSerializerOptions CompactJson = new();
    private readonly AppDbContext _context;
    private readonly ILogger<ApiLogger> _logger;

    public ApiLogger(AppDbContext context, ILogger<ApiLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(ApiLogModel logModel)
    {
        try
        {
            var logEntity = new ApiLog
            {
                API_METHOD_NAME = logModel.MethodName,
                API_PARAMETERS = logModel.Parameters != null
                    ? JsonSerializer.Serialize(logModel.Parameters, CompactJson)
                    : null,
                API_RESPONSE = NormalizeResponse(logModel),
                API_IP_ADDRESS = logModel.IpAddress,
                API_TRACE_ID = logModel.TraceId,
                CREATE_DATE = logModel.CreatedAt
            };

            _context.ApiLogs.Add(logEntity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist API log for {MethodName} with trace ID {TraceId}.",
                logModel.MethodName,
                logModel.TraceId);
        }
    }

    private static string? NormalizeResponse(ApiLogModel logModel)
    {
        if (!string.IsNullOrWhiteSpace(logModel.Response) &&
            TryGetJsonPayload(logModel.Response, out var jsonPayload))
        {
            return jsonPayload;
        }

        if (string.IsNullOrWhiteSpace(logModel.Response) &&
            string.IsNullOrWhiteSpace(logModel.ErrorMessage))
        {
            return null;
        }

        return JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["kind"] = string.IsNullOrWhiteSpace(logModel.Response) ? "empty" : "nonJson",
            ["errorMessage"] = logModel.ErrorMessage,
            ["raw"] = logModel.Response
        }, CompactJson);
    }

    private static bool TryGetJsonPayload(string response, out string jsonPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            jsonPayload = document.RootElement.GetRawText();
            return true;
        }
        catch (JsonException)
        {
            jsonPayload = string.Empty;
            return false;
        }
    }
}
