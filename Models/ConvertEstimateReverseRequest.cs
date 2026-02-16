using System.Text.Json.Serialization;

namespace Rtbs.Bloxcross.Models;

public class ConvertEstimateReverseRequest
{
    [JsonPropertyName("amount_to_receive")]
    public decimal AmountToReceive { get; set; }

    [JsonPropertyName("from_currency")]
    public string FromCurrency { get; set; } = string.Empty;

    [JsonPropertyName("to_currency")]
    public string ToCurrency { get; set; } = string.Empty;

    [JsonPropertyName("desired_lock_time_in_secs")]
    public int DesiredLockTimeInSecs { get; set; } = 0; // optional, 0 = no lock
}
