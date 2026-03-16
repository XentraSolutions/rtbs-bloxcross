using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rtbs.Bloxcross.Models;

[Table("SETTING")]
public class Setting
{
    [Key]
    [Column("SETTING_ID")]
    public int SettingId { get; set; }

    [Column("SETTING_CODE")]
    public string SettingCode { get; set; } = string.Empty;

    [Column("SETTING_VALUE")]
    public string SettingValue { get; set; } = string.Empty;
}
