namespace Rtbs.Bloxcross.Models;
public class ConvertEstimateRequest
{
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public int DesiredLockTimeInSecs { get; set; } = 0; // 0 = immediate
}
