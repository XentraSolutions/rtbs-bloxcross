using Rtbs.Bloxcross.Models;

public interface IWebhookService
{
    Task<WebhookValidationResult> ValidateAndProcessWebhookAsync();
}

public class WebhookValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public WebhookLog? Log { get; set; }
}