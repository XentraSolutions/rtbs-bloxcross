public interface IBloxService
{
    Task<(bool Success, string? Response, string? ErrorMessage)> GetAsync(string path);
    Task<(bool Success, string? Response, string? ErrorMessage)> PostAsync(string path, object body);
}
