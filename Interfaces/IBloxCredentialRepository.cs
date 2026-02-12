public interface IBloxCredentialRepository
{
    Task<(string BaseUrl, string ClientId, string ApiKey)> GetActiveAsync();
}