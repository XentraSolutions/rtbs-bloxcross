using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rtbs.Bloxcross.Data;

public class BloxCredentialRepository : IBloxCredentialRepository
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    private const string CACHE_KEY = "BLOX_ACTIVE_CREDENTIAL";

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

        var result = (credential.BaseUrl, credential.ClientId, credential.ApiKey, credential.SecretKey);

        _cache.Set(CACHE_KEY, result, TimeSpan.FromMinutes(10));

        return result;
    }
}
