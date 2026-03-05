using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
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
            return new ContentResult
            {
                StatusCode = result.StatusCode,
                ContentType = "application/json",
                Content = JsonSerializer.Serialize(new { success = true, id = result.Log?.Id })
            };
        }

        var payload = JsonSerializer.Serialize(new { error = result.ErrorMessage });
        return new ContentResult
        {
            StatusCode = result.StatusCode,
            ContentType = "application/json",
            Content = payload
        };
    }
}
