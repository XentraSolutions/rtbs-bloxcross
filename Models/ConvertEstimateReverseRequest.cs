namespace Rtbs.Bloxcross.Models
{
    public class ConvertEstimateReverseRequest
    {
        public decimal AmountToReceive { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public int DesiredLockTimeInSecs { get; set; } = 0; // optional, 0 = no lock
    }
}
