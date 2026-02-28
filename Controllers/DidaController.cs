using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class DidaController : ControllerBase
{
    private readonly IBloxService _service;

    public DidaController(IBloxService service)
    {
        _service = service;
    }

    [HttpGet("am_i_enabled")]
    public async Task<IActionResult> AmIEnabled()
    {
        var result = await _service.GetAsync("/accounts/dida_account/am_i_enabled");
        return UpstreamContent(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateDida([FromBody] CreateDidaRequest request)
    {
        var result = await _service.PostAsync("/accounts/dida_account/new", request);
        return UpstreamContent(result);
    }

    [HttpDelete("disable/{account}")]
    public async Task<IActionResult> DisableDida([FromRoute] string account)
    {
        var result = await _service.DeleteAsync($"/accounts/dida_account/account/{account}");
        return UpstreamContent(result);
    }

    [HttpGet("get_account/{account}")]
    public async Task<IActionResult> GetDida([FromRoute] string account)
    {
        var result = await _service.GetAsync($"/accounts/dida_account/get_account/{account}");
        return UpstreamContent(result);
    }

    [HttpGet("get_all")]
    public async Task<IActionResult> GetAllDidas()
    {
        var result = await _service.GetAsync("/accounts/dida_account/get_all");
        return UpstreamContent(result);
    }

    [HttpGet("get_portfolio_balances/{account}")]
    public async Task<IActionResult> GetDidaPortfolioBalance([FromRoute] string account)
    {
        var result = await _service.GetAsync($"/accounts/dida_account/get_portfolio_balances/{account}");
        return UpstreamContent(result);
    }

    [HttpGet("get_transactions/{account}")]
    public async Task<IActionResult> GetDidaTransactions([FromRoute] string account)
    {
        var result = await _service.GetAsync($"/accounts/dida_account/get_transactions/{account}");
        return UpstreamContent(result);
    }

    [HttpPost("wallets/get_dida_inbound_wallet")]
    public async Task<IActionResult> GetDidaInboundWallet([FromBody] GetDidaInboundWalletRequest request)
    {
        var result = await _service.PostAsync("/wallets/get_dida_inbound_wallet", request);
        return UpstreamContent(result);
    }

    [HttpPost("transfer_from_dida")]
    public async Task<IActionResult> TransferFromDida([FromBody] TransferFromDidaRequest request)
    {
        var result = await _service.PostAsync("/accounts/dida_account/transfer_from_dida", request);
        return UpstreamContent(result);
    }

    [HttpGet("rails")]
    public async Task<IActionResult> GetAvailableRails()
    {
        var result = await _service.GetAsync("/accounts/rails");
        return UpstreamContent(result);
    }

    [HttpPost("enable_crypto_rails")]
    public async Task<IActionResult> EnableDidaCryptoRail([FromBody] DidaAccountReferenceRequest request)
    {
        var result = await _service.PostAsync("/accounts/dida_account/enable_crypto_rails", request);
        return UpstreamContent(result);
    }

    [HttpPost("enable_card_rails")]
    public async Task<IActionResult> EnableDidaCardRail([FromBody] DidaAccountReferenceRequest request)
    {
        var result = await _service.PostAsync("/accounts/dida_account/enable_card_rails", request);
        return UpstreamContent(result);
    }

    [HttpPost("enable_local_rails")]
    public async Task<IActionResult> EnableDidaLocalRail([FromBody] DidaAccountReferenceRequest request)
    {
        var result = await _service.PostAsync("/accounts/dida_account/enable_local_rails", request);
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
