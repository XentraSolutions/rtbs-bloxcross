using System.Text.Json.Serialization;

namespace Rtbs.Bloxcross.Models;

public class WebhookEncryptedData
{
    [JsonPropertyName("ivi")]
    public string Iv { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}

public class WebhookEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("base_currency")]
    public string BaseCurrency { get; set; } = string.Empty;

    [JsonPropertyName("transaction_id")]
    public long TransactionId { get; set; }

    [JsonPropertyName("qty")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("feeAmount")]
    public decimal? FeeAmount { get; set; }

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("portfolio_id")]
    public string PortfolioId { get; set; } = string.Empty;

    [JsonPropertyName("deposit_code")]
    public string? DepositCode { get; set; }

    [JsonPropertyName("deposit_id")]
    public string? DepositId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("hold_id")]
    public string? HoldId { get; set; }
}
