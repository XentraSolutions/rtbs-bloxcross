using System.Text.Json.Serialization;

namespace Rtbs.Bloxcross.Models;

public class CreateDidaRequest
{
    [JsonPropertyName("dida_name")]
    public string DidaName { get; set; } = string.Empty;

    [JsonPropertyName("dida_email")]
    public string DidaEmail { get; set; } = string.Empty;

    [JsonPropertyName("dida_phone")]
    public string DidaPhone { get; set; } = string.Empty;

    [JsonPropertyName("kyc_required")]
    public bool KycRequired { get; set; }

    [JsonPropertyName("rails_enabled")]
    public List<DidaRailStateRequest> RailsEnabled { get; set; } = new();

    [JsonPropertyName("account_status")]
    public string AccountStatus { get; set; } = string.Empty;

    [JsonPropertyName("dida_preffered_currency")]
    public string DidaPreferredCurrency { get; set; } = string.Empty;

    [JsonPropertyName("auto_transfer_to_main_portfolio")]
    public bool AutoTransferToMainPortfolio { get; set; }
}

public class DidaRailStateRequest
{
    [JsonPropertyName("rail")]
    public string Rail { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

public class GetDidaInboundWalletRequest
{
    [JsonPropertyName("coin")]
    public string Coin { get; set; } = string.Empty;

    [JsonPropertyName("blockchain")]
    public string Blockchain { get; set; } = string.Empty;

    [JsonPropertyName("didaId")]
    public string DidaId { get; set; } = string.Empty;
}

public class TransferFromDidaRequest
{
    [JsonPropertyName("didaAccountId")]
    public string DidaAccountId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}

public class DidaAccountReferenceRequest
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;
}
