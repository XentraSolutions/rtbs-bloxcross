namespace Rtbs.Bloxcross.Models
{
    public class WebhookRequest
    {
    }
}
public class WebhookEncryptedData
{
    public string Iv { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

public class WebhookEvent
{
    public string EventType { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public long TransactionId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? FeeAmount { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string PortfolioId { get; set; } = string.Empty;
    public string? DepositCode { get; set; }
    public string? DepositId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? HoldId { get; set; }
}

public class WebhookLog
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PortfolioId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}