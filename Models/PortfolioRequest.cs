using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Rtbs.Bloxcross.Models;

public class PortfolioGetBalancesRequest
{
    [JsonPropertyName("portfolio_name")]
    public string PortfolioName { get; set; } = string.Empty;
}

public class PortfolioGetTransactionRequest
{
    [FromRoute(Name = "transactionId")]
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;
}

public class PortfolioGetWebhookEventTypesRequest
{
}

public class PortfolioWebhookSubscribeRequest
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; set; } = string.Empty;
}
