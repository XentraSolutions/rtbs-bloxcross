using Microsoft.EntityFrameworkCore;
using Rtbs.Bloxcross.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<BloxInboundAuthFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<BloxInboundAuthFilter>();
});
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
builder.Services.AddScoped<IWebhookService, WebhookService>();

builder.Services.AddHttpClient<IBloxService, BloxService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.InjectJavascript("/swagger/swagger-auth.js");
});

app.UseHttpsRedirection();

app.UseDeveloperExceptionPage();

app.Use(async (context, next) =>
{
    if (BloxInboundAuthAutoFillMiddleware.ShouldAutoFill(context.Request))
    {
        var repository = context.RequestServices.GetRequiredService<IBloxCredentialRepository>();
        await BloxInboundAuthAutoFillMiddleware.FillMissingHeadersAsync(context.Request, repository);
    }

    await next();
});
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/swagger/auth-headers", async (string method, string path, IBloxCredentialRepository repository) =>
    {
        var credential = await repository.GetActiveAsync();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var normalizedPath = BloxHmacHelper.GetPathForSignature(path);
        var signature = BloxHmacHelper.ComputeSignatureBase64(
            credential.SecretKey,
            timestamp,
            method,
            normalizedPath);

        return Results.Json(new
        {
            apiKey = credential.ApiKey,
            clientId = credential.ClientId,
            timestamp,
            signature
        });
    }).ExcludeFromDescription();
}

app.Run();
