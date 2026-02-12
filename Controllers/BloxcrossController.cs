using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;

[ApiController]
[Route("api/bloxcross")]
public class BloxcrossController : ControllerBase
{
    private readonly IBloxService _service;

    public BloxcrossController(IBloxService service)
    {
        _service = service;
    }

    [HttpPost("swap/convert_estimate")]
    public async Task<IActionResult> ConvertEstimate([FromBody] ConvertEstimateRequest request)
    {
        var result = await _service.PostAsync("/portfolio/swap/convert_estimate", request);

        if (!result.Success)
            return StatusCode(400, new { error = result.ErrorMessage });

        return Ok(result.Response);
    }

    [HttpPost("swap/convert_estimate_reverse")]
    public async Task<IActionResult> ConvertEstimate([FromBody] ConvertEstimateReverseRequest request)
    {
        var result = await _service.PostAsync("/portfolio/swap/convert_estimate_reverse", request);

        if (!result.Success)
            return StatusCode(400, new { error = result.ErrorMessage });

        return Ok(result.Response);
    }
}
