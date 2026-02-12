using Rtbs.Bloxcross.Data;

public class ApiLogger : IApiLogger
{
    private readonly AppDbContext _context;

    public ApiLogger(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(ApiLogModel logModel)
    {
        try
        {
            var logEntity = new ApiLog
            {
                API_METHOD_NAME = logModel.MethodName,
                API_PARAMETERS = logModel.Parameters != null
                    ? System.Text.Json.JsonSerializer.Serialize(logModel.Parameters, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                    : null,
                API_RESPONSE = logModel.Response != null
                    ? System.Text.Json.JsonSerializer.Serialize(logModel.Response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                    : null,
                API_IP_ADDRESS = logModel.IpAddress,
                API_TRACE_ID = logModel.TraceId,
                CREATE_DATE = logModel.CreatedAt
            };

            _context.ApiLogs.Add(logEntity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log API call: {ex}");
        }
    }
}
