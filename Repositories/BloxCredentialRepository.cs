using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rtbs.Bloxcross.Data;

public class BloxCredentialRepository : IBloxCredentialRepository
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    private const string CACHE_KEY = "BLOX_ACTIVE_CREDENTIAL";
    private const string SETTING_CACHE_PREFIX = "BLOX_SETTING_";

    public BloxCredentialRepository(
        AppDbContext context,
        IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<(string BaseUrl, string ClientId, string ApiKey, string SecretKey)> GetActiveAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY, out (string, string, string, string) cached))
            return cached;

        var credential = await _context.BloxCredentials
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new Exception("No active Blox credential found.");

        var result = (
            credential.BaseUrl.Trim(),
            credential.ClientId.Trim(),
            credential.ApiKey.Trim(),
            credential.SecretKey.Trim());

        _cache.Set(CACHE_KEY, result, TimeSpan.FromMinutes(10));

        return result;
    }

    public async Task<string?> GetSettingValueAsync(string settingCode)
    {
        if (string.IsNullOrWhiteSpace(settingCode))
        {
            return null;
        }

        var cacheKey = $"{SETTING_CACHE_PREFIX}{settingCode.Trim().ToUpperInvariant()}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        var settingValue = await _context.Settings
            .Where(x => x.SettingCode == settingCode)
            .Select(x => x.SettingValue)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(settingValue))
        {
            _cache.Set(cacheKey, settingValue, TimeSpan.FromMinutes(10));
        }

        return settingValue;
    }
}
