using System.ComponentModel.DataAnnotations.Schema;

namespace Rtbs.Bloxcross.Models;

[Table("WEBHOOK_LOGS")]
public class WebhookLog
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("EVENT_TYPE")]
    public string EventType { get; set; } = string.Empty;

    [Column("TRANSACTION_ID")]
    public string TransactionId { get; set; } = string.Empty;

    [Column("PORTFOLIO_ID")]
    public string PortfolioId { get; set; } = string.Empty;

    [Column("AMOUNT")]
    public decimal Amount { get; set; }

    [Column("CURRENCY")]
    public string Currency { get; set; } = string.Empty;

    [Column("STATUS")]
    public string Status { get; set; } = string.Empty;

    [Column("RAW_PAYLOAD")]
    public string RawPayload { get; set; } = string.Empty;

    [Column("RECEIVED_AT")]
    public DateTime ReceivedAt { get; set; }

    [Column("PROCESSED_AT")]
    public DateTime? ProcessedAt { get; set; }

    [Column("ERROR_MESSAGE")]
    public string? ErrorMessage { get; set; }
}
