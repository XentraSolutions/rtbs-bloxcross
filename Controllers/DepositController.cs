using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;

[ApiController]
[Route("api/[controller]")]
public class DepositController : ControllerBase
{
    private readonly IBloxService _service;
    public DepositController(IBloxService service)
    {
        _service = service;
    }

    [HttpPost("deposit_account")] //deposit account and deposit instrument
    public async Task<IActionResult> ConvertEstimate([FromBody] DepositRequest request)
    {
        var result = await _service.GetAsync($"/payments/bank-details/account/{request.client_id}?countryCode={request.countryCode}&currency={request.currency}");
        return ApiResponseFactory.FromUpstream(result);
    }
}
