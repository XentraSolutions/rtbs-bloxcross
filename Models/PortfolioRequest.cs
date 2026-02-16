using System.Text.Json.Serialization;

namespace Rtbs.Bloxcross.Models;

public class PortfolioGetBalancesRequest
{
    [JsonPropertyName("portfolio_name")]
    public string PortfolioName { get; set; } = string.Empty;
}