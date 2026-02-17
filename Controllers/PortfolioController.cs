using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;
using System.Text.Json;

[ApiController]
[Route("portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly IBloxService _service;

    public PortfolioController(IBloxService service)
    {
        _service = service;
    }

    [HttpPost("get_portfolio_balances")]
    public async Task<IActionResult> GetPortfolioBalances([FromBody] PortfolioGetBalancesRequest request)
    {
        var result = await _service.PostAsync("/portfolio/get_portfolio_balances", request);
        return UpstreamContent(result);
    }

    [HttpGet("transactions/{transactionId}")]
    public async Task<IActionResult> GetPortfolioTransaction([FromRoute] PortfolioGetTransactionRequest request)
    {
        var result = await _service.GetAsync($"/portfolio/transactions/{request.TransactionId}");
        return UpstreamContent(result);
    }

    [HttpGet("webhooks/eventTypes")]
    public async Task<IActionResult> GetPortfolioWebhookEventTypes([FromQuery] PortfolioGetWebhookEventTypesRequest request)
    {
        _ = request;
        var result = await _service.GetAsync("/portfolio/webhooks/eventTypes");
        return UpstreamContent(result);
    }

    [HttpPost("webhooks/subscribe")]
    public async Task<IActionResult> SubscribePortfolioWebhook([FromBody] PortfolioWebhookSubscribeRequest request)
    {
        var result = await _service.PostAsync("/portfolio/webhooks/subscribe", request);
        return UpstreamContent(result);
    }

    private static IActionResult UpstreamContent((bool Success, int StatusCode, string? Response, string? ErrorMessage) result)
    {
        if (result.Response is not null)
        {
            return new ContentResult
            {
                StatusCode = result.StatusCode,
                ContentType = "application/json",
                Content = result.Response
            };
        }

        var payload = JsonSerializer.Serialize(new { error = result.ErrorMessage ?? "Upstream request failed." });

        return new ContentResult
        {
            StatusCode = result.StatusCode,
            ContentType = "application/json",
            Content = payload
        };
    }
}
