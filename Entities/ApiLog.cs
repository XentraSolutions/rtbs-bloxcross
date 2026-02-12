using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("API_LOG")]
public class ApiLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong API_ID { get; set; }

    [StringLength(70)]
    public string? API_METHOD_NAME { get; set; }

    public string? API_PARAMETERS { get; set; }

    public string? API_RESPONSE { get; set; }

    [StringLength(30)]
    public string? API_IP_ADDRESS { get; set; }

    [StringLength(50)]
    public string? API_TRACE_ID { get; set; }  // ✅ matches DB column

    public DateTime CREATE_DATE { get; set; } = DateTime.UtcNow;
}
