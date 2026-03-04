using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;

[ApiController]
[Route("api/[controller]")]
public class DepositController : DidaController
{
    private readonly IBloxService _service;
    public DepositController(IBloxService service) : base(service)
    {
        _service = service;
    }

    [HttpPost("deposit_account")] //deposit account and deposit instrument
    public async Task<IActionResult> ConvertEstimate([FromBody] DepositRequest request)
    {
        var result = await _service.GetAsync($"/payments/bank-details/account/{request.client_id}?countryCode=US&currency=USD");
        return UpstreamContent(result);
    }
}