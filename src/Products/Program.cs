using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Products.Data;
using Products.Endpoints;
using Products.Services;

var builder = WebApplication.CreateBuilder(args);
var enableHttpsRedirection = builder.Configuration.GetValue("EnableHttpsRedirection", true);

builder.AddServiceDefaults();

builder.Services.Configure<AgentAccessOptions>(builder.Configuration.GetSection(AgentAccessOptions.SectionName));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("agent-api", context =>
    {
        var configuredLimit = context.RequestServices.GetRequiredService<IOptions<AgentAccessOptions>>().Value.RequestsPerMinute;
        var permitLimit = configuredLimit <= 0 ? 60 : configuredLimit;
        var partitionKey = context.Request.Headers["X-Agent-Id"].ToString();
        partitionKey = string.IsNullOrWhiteSpace(partitionKey) ? "anonymous-agent" : partitionKey;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// Configure SQL Server connection
var connectionString = builder.Configuration.GetConnectionString("TinyShopDB");
var connectionStringSource = "ConnectionStrings:TinyShopDB";

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = builder.Configuration["PRODUCTS_DB_CONNECTION_STRING"];
    connectionStringSource = "PRODUCTS_DB_CONNECTION_STRING";
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No SQL connection string configured. Set ConnectionStrings:TinyShopDB or PRODUCTS_DB_CONNECTION_STRING.");
}

var connectionStringInfo = GetConnectionStringInfo(connectionString);

builder.Services.AddDbContext<ProductDataContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("ProductsTestDb");
    }
    else
    {
        options.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null));
    }
});

// Add embedding service for semantic search
builder.Services.AddHttpClient<IEmbeddingService, LocalEmbeddingService>();
builder.Services.AddScoped<ProductSearchService>();

// Add CORS policy for Blazor Server frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.AllowAnyOrigin()
  .AllowAnyMethod()
          .AllowAnyHeader();
    });
});

// Add services to the container.
var app = builder.Build();

app.Logger.LogInformation(
    "Products DB connection resolved from {Source}. Target server: {Server}. Database: {Database}.",
    connectionStringSource,
    connectionStringInfo.Server,
    connectionStringInfo.Database);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// Enable CORS before other middleware
app.UseCors("AllowBlazorClient");
app.UseRateLimiter();

// Static files must be configured before routing
app.UseStaticFiles();

app.MapProductEndpoints();
app.MapCustomerOrderEndpoints();
app.MapAgentCommerceEndpoints();

// Initialize the database and generate embeddings on startup.
await WaitForSqlServerAsync(connectionString, app.Logger);
await app.InitializeDatabaseAsync();

app.Run();

static async Task WaitForSqlServerAsync(string connectionString, ILogger logger)
{
    var builder = new SqlConnectionStringBuilder(connectionString);
    var dataSource = builder.DataSource?.Trim();

    if (string.IsNullOrWhiteSpace(dataSource))
    {
        return;
    }

    var host = dataSource;
    var delimiterIndex = host.IndexOf(',');
    if (delimiterIndex >= 0)
    {
        host = host[..delimiterIndex];
    }

    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "::1")
    {
        return;
    }

    var timeout = TimeSpan.FromSeconds(60);
    var stopAt = DateTime.UtcNow + timeout;
    var attempt = 0;

    while (true)
    {
        attempt++;

        try
        {
            logger.LogInformation("Checking SQL Server host resolution for '{Host}' (attempt {Attempt})...", host, attempt);
            var addresses = await Dns.GetHostAddressesAsync(host);
            if (addresses.Length > 0)
            {
                logger.LogInformation("SQL Server host '{Host}' resolved successfully to {Addresses}.", host, string.Join(',', addresses));
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SQL Server host '{Host}' could not be resolved on attempt {Attempt}.", host, attempt);
        }

        if (DateTime.UtcNow >= stopAt)
        {
            logger.LogError("Timed out waiting for SQL Server host '{Host}' to resolve after {Timeout}s.", host, timeout.TotalSeconds);
            break;
        }

        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

static (string Server, string Database) GetConnectionStringInfo(string value)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(value);
        return (builder.DataSource, builder.InitialCatalog);
    }
    catch
    {
        return ("<unparsed>", "<unparsed>");
    }
}

namespace Products
{
    // Expose Program for integration tests
    public partial class Program { }
}