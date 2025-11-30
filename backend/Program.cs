// Add this to your backend.csproj
// <PackageReference Include="Confluent.Kafka" Version="2.3.0" />

using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.GraphQL;
using backend.Services; // Add Kafka service
using StackExchange.Redis;
using Confluent.Kafka;
using System;
using HotChocolate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using backend.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ... [Keep your existing DATABASE_URL setup] ...

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                  ?? builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"🔍 DATABASE_URL exists: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))}");

string connectionString;

if (!string.IsNullOrEmpty(databaseUrl) && (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://")))
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
       // Extract port first
var port = uri.Port > 0 ? uri.Port : 5432;

// Then use it in the connection string
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    )
);

// ... [Keep your existing Redis setup] ...

var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL") 
               ?? builder.Configuration.GetConnectionString("Redis") 
               ?? "localhost:6379";

if (redisUrl.StartsWith("redis://"))
{
    redisUrl = redisUrl.Substring("redis://".Length);
}

IConnectionMultiplexer redisConnection = null;

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var configOptions = ConfigurationOptions.Parse(redisUrl);
        configOptions.AbortOnConnectFail = false;
        configOptions.ConnectTimeout = 5000;
        configOptions.SyncTimeout = 5000;
        configOptions.ConnectRetry = 3;
        configOptions.KeepAlive = 60;

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
        
        Console.WriteLine("⚠️ Redis connection could not be established");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Redis connection warning: {ex.Message}");
        return null;
    }
});

// ===== KAFKA CONFIGURATION =====
var kafkaBootstrapServers = builder.Configuration.GetConnectionString("Kafka") 
                             ?? "localhost:9092";

Console.WriteLine($"🔍 Kafka Bootstrap: {kafkaBootstrapServers}");

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    try
    {
        var config = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            ClientId = "graphql-api",
            Acks = Acks.Leader,
            MessageTimeoutMs = 5000
        };

        var producer = new ProducerBuilder<string, string>(config).Build();
        Console.WriteLine("✅ Kafka Producer initialized");
        return producer;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Kafka warning: {ex.Message}");
        return null;
    }
});

builder.Services.AddSingleton<IKafkaService, KafkaService>();

// ===== KEYCLOAK CONFIGURATION =====
var keycloakSettings = builder.Configuration
    .GetSection("Keycloak")
    .Get<KeycloakSettings>();
    
if (keycloakSettings == null)
{
    throw new InvalidOperationException("Keycloak configuration is missing");
}

builder.Services.AddSingleton(keycloakSettings);

// ... [Keep your existing CORS, Auth, and GraphQL setup] ...

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "http://localhost:3000"
    };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakSettings.Authority;
        options.Audience = keycloakSettings.Audience;
        options.RequireHttpsMetadata = false;
        options.MetadataAddress = $"{keycloakSettings.Authority}/.well-known/openid-configuration";
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = keycloakSettings.Audience,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

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

// ... [Keep your existing migration code] ...

app.UseCors();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

// Kafka endpoints
app.MapGet("/health/kafka", (IKafkaService kafka) =>
{
    var status = kafka.IsHealthy() ? "healthy" : "degraded";
    return Results.Ok(new { status, service = "kafka" });
});

app.MapPost("/kafka/send", async (IKafkaService kafka, string topic, string message) =>
{
    var result = await kafka.ProduceMessageAsync(topic, Guid.NewGuid().ToString(), message);
    return result ? Results.Ok("Message sent") : Results.Problem("Failed to send");
}).AllowAnonymous();

app.MapGet("/kafka/topics", async (IKafkaService kafka) =>
{
    var topics = await kafka.GetTopicsAsync();
    return Results.Ok(topics);
}).AllowAnonymous();

app.MapGet("/kafka/consume/{topic}", async (IKafkaService kafka, string topic) =>
{
    var messages = await kafka.ConsumeMessagesAsync(topic, 10);
    return Results.Ok(messages);
}).AllowAnonymous();


// Test Kafka Endpoint
app.MapPost("/test-kafka", async (IKafkaService kafkaService) =>
{
    try
    {
        var testMessage = new
        {
            id = Guid.NewGuid(),
            message = "Test message from API",
            timestamp = DateTime.UtcNow
        };

        var result = await kafkaService.ProduceMessageAsync(
            "test-topic", 
            testMessage.id.ToString(), 
            System.Text.Json.JsonSerializer.Serialize(testMessage)
        );

        if (result)
        {
            return Results.Ok(new { success = true, message = "Message sent to Kafka" });
        }
        else
        {
            return Results.Ok(new { success = false, message = "Failed to send message" });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).AllowAnonymous();

app.MapGet("/health", () => 
{
    var redisStatus = redisConnection?.IsConnected == true ? "healthy" : "degraded";
    
    return Results.Ok(new 
    { 
        status = "healthy", 
        redis = redisStatus,
        timestamp = DateTime.UtcNow
    });
});

Console.WriteLine("🚀 Application starting...");
Console.WriteLine($"📍 GraphQL endpoint: /graphql");
Console.WriteLine($"💊 Health check: GET /health");
Console.WriteLine($"💊 Kafka health: GET /health/kafka");
Console.WriteLine($"🧪 Test Kafka: POST /test-kafka");

app.Run();