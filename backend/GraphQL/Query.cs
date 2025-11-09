using Microsoft.EntityFrameworkCore;
using HotChocolate;
using HotChocolate.Data;
using StackExchange.Redis;
using System.Text.Json;
using backend.Models;
using backend.Data;
using HotChocolate.Authorization;

namespace backend.GraphQL;

public class Query
{
    // Get all users with filtering, sorting, and caching
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [Authorize]
    public IQueryable<User> GetUsers([Service] ApplicationDbContext context)
    {
        return context.Users;
    }


    // Get user by ID with Redis caching
    [GraphQLName("userById")]  // Add this
    [Authorize]
    public async Task<User?> GetUserById(
                    int id,
                            [Service] ApplicationDbContext context,
                                    [Service] IConnectionMultiplexer redis,
                                            CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var cacheKey = $"user:{id}";

        // Try to get from cache
        var cachedUser = await db.StringGetAsync(cacheKey);

        if (!cachedUser.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<User>(cachedUser!);
        }

        // If not in cache, get from database
        var user = await context.Users.FindAsync(new object[] { id }, cancellationToken);

        //Cache the result for 5 minutes
        if (user != null)
        {
            var serializedUser = JsonSerializer.Serialize(user);
            await db.StringSetAsync(cacheKey, serializedUser, TimeSpan.FromMinutes(5));
        }

        return user;
    }

    // Get user by email with caching
    [GraphQLName("userByEmail")]  // Add this for consistency
    [Authorize]
    public async Task<User?> GetUserByEmail(
                    string email,
                            [Service] ApplicationDbContext context,
                                    [Service] IConnectionMultiplexer redis,
                                            CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var cacheKey = $"user:email:{email}";

        var cachedUser = await db.StringGetAsync(cacheKey);

        if (!cachedUser.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<User>(cachedUser!);
        }

        var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user != null)
        {
            var serializedUser = JsonSerializer.Serialize(user);
            await db.StringSetAsync(cacheKey, serializedUser, TimeSpan.FromMinutes(5));
        }

        return user;
    }
}