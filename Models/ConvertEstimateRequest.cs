using System.Text.Json.Serialization;

namespace Rtbs.Bloxcross.Models;

public class ConvertEstimateRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("from_currency")]
    public string FromCurrency { get; set; } = string.Empty;

    [JsonPropertyName("to_currency")]
    public string ToCurrency { get; set; } = string.Empty;

    [JsonPropertyName("desired_lock_time_in_secs")]
    public int DesiredLockTimeInSecs { get; set; } = 0; // 0 = immediate
}
