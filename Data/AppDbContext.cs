using Microsoft.EntityFrameworkCore;
using Rtbs.Bloxcross.Models;

namespace Rtbs.Bloxcross.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
    public DbSet<BloxCredential> BloxCredentials => Set<BloxCredential>();
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();
}
