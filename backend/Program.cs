using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.GraphQL;
using StackExchange.Redis;
using System;

var builder = WebApplication.CreateBuilder(args);

// Get DATABASE_URL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                  ?? builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"🔍 DATABASE_URL exists: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))}");

string connectionString;

// Convert postgres:// URL to Npgsql connection string format
if (!string.IsNullOrEmpty(databaseUrl) && (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://")))
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        
        Console.WriteLine("✅ Converted DATABASE_URL to Npgsql format");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to parse DATABASE_URL: {ex.Message}");
        throw;
    }
}
else
{
    connectionString = databaseUrl;
    Console.WriteLine("✅ Using connection string as-is");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string not found");
}

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    )
);

var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL") 
               ?? builder.Configuration.GetConnectionString("Redis") 
               ?? "localhost:6379";

// Remove redis:// prefix if present
if (redisUrl.StartsWith("redis://"))
{
    redisUrl = redisUrl.Substring("redis://".Length);
}

Console.WriteLine($"🔍 Connecting to Redis: {redisUrl}");

// Add Redis with retry and timeout settings
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var configOptions = ConfigurationOptions.Parse(redisUrl);
        configOptions.AbortOnConnectFail = false; // Don't crash on startup
        configOptions.ConnectTimeout = 10000; // 10 seconds
        configOptions.SyncTimeout = 5000;
        configOptions.ConnectRetry = 3;
        configOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);
        
        var redis = ConnectionMultiplexer.Connect(configOptions);
        Console.WriteLine("✅ Redis connected successfully");
        return redis;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Redis connection failed: {ex.Message}");
        throw;
    }
});

// Add GraphQL with HotChocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddType<UserType>()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .AddRedisSubscriptions(sp => sp.GetRequiredService<IConnectionMultiplexer>())
.ModifyRequestOptions(opt => 
    {
        opt.IncludeExceptionDetails = true; // ADD THIS LINE
    });
// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
         policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine("🔄 Applying database migrations...");
        context.Database.Migrate();
        Console.WriteLine("✅ Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ An error occurred while migrating the database: {ex.Message}");
        Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
    }
}

app.UseCors();
app.UseWebSockets();
app.MapGraphQL();

app.MapGet("/", () => Results.Redirect("/graphql"));

// In Program.cs after var app = builder.Build();
app.MapGet("/health/redis", async (IConnectionMultiplexer redis) =>
{
    try
    {
        var db = redis.GetDatabase();
        await db.PingAsync();
        return Results.Ok("Redis connected");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Redis error: {ex.Message}");
    }
});

Console.WriteLine("🚀 Application starting...");
app.Run();