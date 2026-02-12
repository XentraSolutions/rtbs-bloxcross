namespace Rtbs.Bloxcross.Models;

public class Deposit
{
    public Guid Id { get; set; }
    public string Currency { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
