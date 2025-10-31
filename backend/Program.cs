using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.GraphQL;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    )
);

// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// Add GraphQL with HotChocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddType<UserType>()
    // Add filtering, sorting, and projections
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    // Use Redis for subscriptions (production-ready, scales across multiple instances)
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
        context.Database.Migrate();
        Console.WriteLine("✅ Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ An error occurred while migrating the database: {ex.Message}");
    }
}

app.UseCors();
app.UseWebSockets();
app.MapGraphQL();

app.MapGet("/", () => Results.Redirect("/graphql"));

app.Run();