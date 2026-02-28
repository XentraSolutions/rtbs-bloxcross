using System.ComponentModel.DataAnnotations.Schema;

namespace Rtbs.Bloxcross.Models;

[Table("BLOX_CREDENTIALS")]
public class BloxCredential
{
    [Column("ID")]
    public int Id { get; set; }

    [Column("NAME")]
    public string Name { get; set; } = string.Empty;

    [Column("BASE_URL")]
    public string BaseUrl { get; set; } = string.Empty;

    [Column("CLIENT_ID")]
    public string ClientId { get; set; } = string.Empty;

    [Column("API_KEY")]
    public string ApiKey { get; set; } = string.Empty;
    [Column("SECRET_KEY")]
    public string SecretKey { get; set; } = string.Empty;

    [Column("IS_ACTIVE")]
    public bool IsActive { get; set; }

    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }
}
