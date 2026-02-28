using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class ConvertController : ControllerBase
{
    private readonly IBloxService _service;

    public ConvertController(IBloxService service)
    {
        _service = service;
    }

    [HttpPost("convert_estimate")]
    public async Task<IActionResult> ConvertEstimate([FromBody] ConvertEstimateRequest request)
    {
        var result = await _service.PostAsync("/portfolio/swap/convert_estimate", request);
        return UpstreamContent(result);
    }

    [HttpPost("convert_estimate_reverse")]
    public async Task<IActionResult> ConvertEstimateReverse([FromBody] ConvertEstimateReverseRequest request)
    {
        var result = await _service.PostAsync("/portfolio/swap/convert_estimate_reverse", request);
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
