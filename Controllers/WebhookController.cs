using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
[SkipBloxInboundAuth]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhookController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        var result = await _webhookService.ValidateAndProcessWebhookAsync();
        return WebhookResponse(result);
    }

    private static IActionResult WebhookResponse(WebhookValidationResult result)
    {
        if (result.IsValid)
        {
            if (result.IsDuplicate)
            {
                return ApiResponseFactory.Success(
                    result.StatusCode,
                    "Webhook already processed.",
                    new { duplicate = true });
            }

            return ApiResponseFactory.Success(
                result.StatusCode,
                "Webhook processed successfully.",
                new { id = result.Log?.Id });
        }

        return ApiResponseFactory.Failure(
            result.StatusCode,
            result.ErrorMessage ?? "Webhook processing failed.");
    }
}
