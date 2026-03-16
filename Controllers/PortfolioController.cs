using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IBloxService _service;
    private readonly IBloxCredentialRepository _repository;

    public PortfolioController(IBloxService service, IBloxCredentialRepository repository)
    {
        _service = service;
        _repository = repository;
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
        if (string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            request.CallbackUrl = await _repository.GetSettingValueAsync("CALL_BACK_URL") ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            return ApiResponseFactory.Failure(400, "CALL_BACK_URL setting is missing.");
        }

        var result = await _service.PostAsync("/portfolio/webhooks/subscribe", request);
        return UpstreamContent(result);
    }

    private static IActionResult UpstreamContent((bool Success, int StatusCode, string? Response, string? ErrorMessage) result)
    {
        return ApiResponseFactory.FromUpstream(result);
    }
}
