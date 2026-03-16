public interface IBloxCredentialRepository
{
    Task<(string BaseUrl, string ClientId, string ApiKey, string SecretKey)> GetActiveAsync();
    Task<string?> GetSettingValueAsync(string settingCode);
}
