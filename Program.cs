using Microsoft.EntityFrameworkCore;
using Rtbs.Bloxcross.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Read configuration (works for launchSettings + production env)
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPass = Environment.GetEnvironmentVariable("DB_PASS");

if (builder.Environment.IsProduction())
{
    if (string.IsNullOrEmpty(dbHost))
        throw new Exception("Database configuration is missing.");
}

// Build MySQL connection string
var connectionString =
    $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};";

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));


builder.Services.AddMemoryCache();

builder.Services.AddScoped<IApiLogger, ApiLogger>();
builder.Services.AddScoped<IBloxCredentialRepository, BloxCredentialRepository>();

builder.Services.AddHttpClient<IBloxService, BloxService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseDeveloperExceptionPage();
app.MapControllers();

app.Run();
