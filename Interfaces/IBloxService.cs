public interface IBloxService
{
    Task<(bool Success, int StatusCode, string? Response, string? ErrorMessage)> GetAsync(string path);
    Task<(bool Success, int StatusCode, string? Response, string? ErrorMessage)> PostAsync(string path, object body);
    Task<(bool Success, int StatusCode, string? Response, string? ErrorMessage)> DeleteAsync(string path);
}
