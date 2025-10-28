using Microsoft.EntityFrameworkCore;
using HotChocolate.Subscriptions;
using StackExchange.Redis;
using backend.Models;
using backend.Data;

namespace backend.GraphQL;
public class Mutation
{
    // Create user
    public async Task<User> CreateUser(
        string name,
        string email,
        [Service] ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var user = new User
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        // Trigger subscription
        await eventSender.SendAsync(nameof(Subscription.OnUserCreated), user, cancellationToken);
        await eventSender.SendAsync(nameof(Subscription.OnUserChanged), user, cancellationToken);

        return user;
    }

    // Update user
    public async Task<User?> UpdateUser(
        int id,
        string? name,
        string? email,
        [Service] ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync(new object[] { id }, cancellationToken);

        if (user == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(name))
        {
            user.Name = name;
        }

        if (!string.IsNullOrEmpty(email))
        {
            user.Email = email;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync($"user:{id}");
        await db.KeyDeleteAsync($"user:email:{email}");

        // Trigger subscription
        await eventSender.SendAsync(nameof(Subscription.OnUserUpdated), user, cancellationToken);
        await eventSender.SendAsync(nameof(Subscription.OnUserChanged), user, cancellationToken);

        return user;
    }

    // Delete user
    public async Task<bool> DeleteUser(
        int id,
        [Service] ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync(new object[] { id }, cancellationToken);

        if (user == null)
        {
            return false;
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync($"user:{id}");
        await db.KeyDeleteAsync($"user:email:{user.Email}");

        // Trigger subscription
        await eventSender.SendAsync(nameof(Subscription.OnUserDeleted), user, cancellationToken);
        await eventSender.SendAsync(nameof(Subscription.OnUserChanged), user, cancellationToken);

        return true;
    }
}