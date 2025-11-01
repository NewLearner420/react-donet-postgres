using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.GraphQL;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Get connection strings from environment variables
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";

Console.WriteLine($"🔍 DATABASE_URL exists: {!string.IsNullOrEmpty(databaseUrl)}");
Console.WriteLine($"🔍 REDIS_URL exists: {!string.IsNullOrEmpty(redisUrl)}");

if (string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("❌ DATABASE_URL not found! Application will fail.");
    throw new InvalidOperationException("DATABASE_URL environment variable is required");
}

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        databaseUrl,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    )
);

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisUrl));

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
    .AddRedisSubscriptions(sp => sp.GetRequiredService<IConnectionMultiplexer>());

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

Console.WriteLine("🚀 Application starting...");
app.Run();