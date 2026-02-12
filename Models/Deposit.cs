using System.ComponentModel.DataAnnotations.Schema;

namespace Rtbs.Bloxcross.Models;

[Table("DEPOSITS")]
public class Deposit
{
    [Column("ID")]
    public Guid Id { get; set; }

    [Column("CURRENCY")]
    public string Currency { get; set; } = default!;

    [Column("AMOUNT")]
    public decimal Amount { get; set; }

    [Column("STATUS")]
    public string Status { get; set; } = default!;

    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
