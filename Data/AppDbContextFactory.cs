using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rtbs.Bloxcross.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "BLOXCROSS";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
        var dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? "";

        var connectionString =
            $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString)
        );

        return new AppDbContext(optionsBuilder.Options);
    }
}
