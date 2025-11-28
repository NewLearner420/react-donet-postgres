using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.GraphQL;
using StackExchange.Redis;
using System;
using HotChocolate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using backend.Configuration;

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
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;
        
        connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        
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

IConnectionMultiplexer redisConnection = null;

// Add Redis with better timeout and retry settings
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var configOptions = ConfigurationOptions.Parse(redisUrl);
        
        // Better settings for free tier stability
        configOptions.AbortOnConnectFail = false;
        configOptions.ConnectTimeout = 5000;       // 5 seconds to connect
        configOptions.SyncTimeout = 5000;          // 5 seconds for sync operations
        configOptions.ConnectRetry = 3;            // Retry 3 times
        configOptions.DefaultDatabase = 0;
        
        // Enable keep-alive to detect stale connections
        configOptions.KeepAlive = 60;

        // Use async connect with timeout to prevent hanging
        var connectTask = ConnectionMultiplexer.ConnectAsync(configOptions);
        if (connectTask.Wait(TimeSpan.FromSeconds(10)))
        {
            redisConnection = connectTask.Result;
            if (redisConnection?.IsConnected == true)
            {
                Console.WriteLine("✅ Redis connected successfully");
                return redisConnection;
            }
        }
        
        Console.WriteLine("⚠️ Redis connection could not be established, running without Redis");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Redis connection warning: {ex.Message}");
        // Don't throw - allow app to start even if Redis is temporarily unavailable
        return null;
    }
});

// Configure Keycloak settings
var keycloakSettings = builder.Configuration
    .GetSection("Keycloak")
    .Get<KeycloakSettings>();
    
if (keycloakSettings == null)
{
    throw new InvalidOperationException("Keycloak configuration is missing in appsettings.json");
}

builder.Services.AddSingleton(keycloakSettings);

// CORS Configuration
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "https://chilling-spooky-crypt-q75pxx9xp4x5h96qw-3000.app.github.dev",
        "http://localhost:3000"
    };

Console.WriteLine($"🔍 Configured CORS origins: {string.Join(", ", allowedOrigins)}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakSettings.Authority;
        options.Audience = keycloakSettings.Audience;
        options.RequireHttpsMetadata = false; // Important for Codespaces
        
        // Explicitly set metadata address
        options.MetadataAddress = $"{keycloakSettings.Authority}/.well-known/openid-configuration";
        
        // Configure HTTP handler for certificate validation
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        
        options.BackchannelTimeout = TimeSpan.FromSeconds(30);
        options.SaveToken = false;
        options.IncludeErrorDetails = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Accept both internal and external issuer
            ValidIssuers = new[]
            {
                "http://keycloak:8080/realms/myrealm",
                "https://chilling-spooky-crypt-q75pxx9xp4x5h96qw-8090.app.github.dev/realms/myrealm"
            },
            ValidAudience = keycloakSettings.Audience,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
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
    .AddAuthorization()
    .AddRedisSubscriptions(sp => sp.GetRequiredService<IConnectionMultiplexer>())
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
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

// Middleware Pipeline - ORDER IS CRITICAL
// 1. CORS must be first
app.UseCors();

// 2. WebSockets
app.UseWebSockets();

// 3. CORS preflight debug middleware
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        Console.WriteLine($"🔍 OPTIONS (preflight) request to: {context.Request.Path}");
        Console.WriteLine($"🔍 Origin: {context.Request.Headers["Origin"]}");
        Console.WriteLine($"🔍 Access-Control-Request-Method: {context.Request.Headers["Access-Control-Request-Method"]}");
        Console.WriteLine($"🔍 Access-Control-Request-Headers: {context.Request.Headers["Access-Control-Request-Headers"]}");
    }
    
    await next();
    
    if (context.Request.Method == "OPTIONS")
    {
        Console.WriteLine($"✅ OPTIONS response status: {context.Response.StatusCode}");
        Console.WriteLine($"✅ Access-Control-Allow-Origin: {context.Response.Headers["Access-Control-Allow-Origin"]}");
        Console.WriteLine($"✅ Access-Control-Allow-Credentials: {context.Response.Headers["Access-Control-Allow-Credentials"]}");
    }
});

// 4. Request logging middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/graphql") && context.Request.Method == "POST")
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        Console.WriteLine($"🔍 POST /graphql request");
        Console.WriteLine($"🔍 Auth Header Present: {!string.IsNullOrEmpty(authHeader)}");
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.Length > 20)
        {
            Console.WriteLine($"🔍 Auth Header (truncated): {authHeader.Substring(0, Math.Min(70, authHeader.Length))}...");
        }
    }

    await next();
});

// 5. Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Map endpoints
app.MapGraphQL();

// Debug endpoint to inspect token
app.MapPost("/debug-token", async (HttpContext context) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        return Results.BadRequest(new { error = "No bearer token provided" });
    }
    
    var token = authHeader.Substring(7).Trim();
    
    // Detailed token analysis
    var bytes = System.Text.Encoding.UTF8.GetBytes(token);
    var hasNonAscii = bytes.Any(b => b > 127);
    var hasControlChars = token.Any(c => char.IsControl(c));
    
    var dotCount = token.Count(c => c == '.');
    var dotPositions = new List<int>();
    for (int i = 0; i < token.Length; i++)
    {
        if (token[i] == '.') dotPositions.Add(i);
    }
    
    return Results.Ok(new
    {
        tokenLength = token.Length,
        dotCount = dotCount,
        dotPositions = dotPositions,
        hasNonAscii = hasNonAscii,
        hasControlChars = hasControlChars,
        firstDotCharCode = dotCount > 0 ? (int)token[dotPositions[0]] : 0,
        secondDotCharCode = dotCount > 1 ? (int)token[dotPositions[1]] : 0,
        first50Chars = token.Substring(0, Math.Min(50, token.Length)),
        around1stDot = dotCount > 0 && dotPositions[0] >= 5 
            ? token.Substring(dotPositions[0] - 5, Math.Min(11, token.Length - (dotPositions[0] - 5))) 
            : "",
        around2ndDot = dotCount > 1 && dotPositions[1] >= 5 
            ? token.Substring(dotPositions[1] - 5, Math.Min(11, token.Length - (dotPositions[1] - 5))) 
            : "",
        parts = token.Split('.').Select(p => new { length = p.Length, preview = p.Substring(0, Math.Min(20, p.Length)) }).ToArray()
    });
}).AllowAnonymous();

app.MapPost("/test-token", async (HttpContext context) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    Console.WriteLine($"🧪 Test endpoint - Auth header received");
    
    if (authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring(7).Trim();
        Console.WriteLine($"🧪 Token length: {token.Length}");
        Console.WriteLine($"🧪 Token has dots: {token.Contains('.')}");
        Console.WriteLine($"🧪 Token dot count: {token.Count(c => c == '.')}");
        Console.WriteLine($"🧪 First 100 chars: {token.Substring(0, Math.Min(100, token.Length))}");
        
        return Results.Ok(new { 
            length = token.Length,
            hasDots = token.Contains('.'),
            dotCount = token.Count(c => c == '.'),
            preview = token.Substring(0, Math.Min(50, token.Length))
        });
    }
    
    return Results.BadRequest("No bearer token");
}).AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/graphql"));

app.MapGet("/diagnose-redis", async (HttpContext context) =>
{
    var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL") 
                   ?? "Not set";
    
    var diagnostics = new
    {
        redisUrlEnvVar = redisUrl,
        redisUrlConfigured = !string.IsNullOrEmpty(redisUrl),
        hostname = redisUrl.Contains("@") ? redisUrl.Split("@")[1].Split(":")[0] : "N/A",
        port = redisUrl.Contains(":6379") ? 6379 : 0000,
        timestamp = DateTime.UtcNow
    };
    
    return Results.Ok(diagnostics);
}).AllowAnonymous();

app.MapGet("/health/redis", async (IConnectionMultiplexer redis) =>
{
    try
    {
        if (redis == null)
        {
            return Results.Ok(new 
            { 
                status = "degraded", 
                service = "redis",
                message = "Redis not initialized (running in degraded mode)",
                timestamp = DateTime.UtcNow
            });
        }
        
        if (!redis.IsConnected)
        {
            return Results.Ok(new 
            { 
                status = "unavailable", 
                service = "redis",
                message = "Redis connection established but not currently connected",
                timestamp = DateTime.UtcNow
            });
        }
        
        var db = redis.GetDatabase();
        var pingTask = db.PingAsync();
        
        if (pingTask.Wait(TimeSpan.FromSeconds(3)))
        {
            return Results.Ok(new 
            { 
                status = "healthy", 
                service = "redis",
                timestamp = DateTime.UtcNow
            });
        }
        else
        {
            return Results.Ok(new 
            { 
                status = "timeout", 
                service = "redis",
                message = "Redis ping timed out",
                timestamp = DateTime.UtcNow
            });
        }
    }
    catch (Exception ex)
    {
        return Results.Ok(new 
        { 
            status = "error", 
            service = "redis",
            message = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
});

app.MapGet("/health", () => 
{
    var redisStatus = redisConnection?.IsConnected == true ? "healthy" : "degraded";
    
    return Results.Ok(new 
    { 
        status = "healthy", 
        redis = redisStatus,
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    });
});

Console.WriteLine("🚀 Application starting...");
Console.WriteLine($"🌐 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔒 HTTPS Metadata Required: {keycloakSettings.RequireHttpsMetadata}");
Console.WriteLine($"🔑 Keycloak Authority: {keycloakSettings.Authority}");
Console.WriteLine($"🎯 Expected Audience: {keycloakSettings.Audience}");
Console.WriteLine($"📍 GraphQL endpoint: /graphql");
Console.WriteLine($"🔍 Debug endpoint: POST /debug-token");
Console.WriteLine($"💊 Health check: GET /health");
Console.WriteLine($"💊 Redis health: GET /health/redis");
Console.WriteLine($"🔍 Connecting to Redis: {redisUrl}");

app.Run();