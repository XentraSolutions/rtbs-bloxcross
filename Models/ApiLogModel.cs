public class ApiLogModel
{
    public string MethodName { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public object? Response { get; set; }
    public string? IpAddress { get; set; }
    public string TraceId { get; set; } = Guid.NewGuid().ToString();  // model property
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
