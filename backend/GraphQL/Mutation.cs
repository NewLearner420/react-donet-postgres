using Microsoft.EntityFrameworkCore;
using HotChocolate.Subscriptions;
using StackExchange.Redis;
using backend.Models;
using backend.Data;
using HotChocolate.Authorization;
using backend.Services;

namespace backend.GraphQL;

public class Mutation
{
    // Create user
    [Authorize]
    public async Task<User> CreateUser(
        string name,
        string email,
        [Service] ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        [Service] ILogger<Mutation> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Creating user: {Name}, {Email}", name, email);

            var user = new User
            {
                Name = name,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            logger.LogInformation("Saving to database...");
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("User saved with ID: {Id}", user.Id);

            // Trigger subscription with timeout and error handling
            try
            {
                logger.LogInformation("Sending subscription events...");
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3)); // 3 second timeout for subscription

                await eventSender.SendAsync(
                    nameof(Subscription.OnUserCreated), 
                    user, 
                    timeoutCts.Token);
                
                await eventSender.SendAsync(
                    nameof(Subscription.OnUserChanged), 
                    user, 
                    timeoutCts.Token);
                
                logger.LogInformation("Subscription events sent successfully");
            }
            catch (OperationCanceledException ex)
            {
                // Subscription timeout - log but don't fail the mutation
                logger.LogWarning(ex, "Subscription event timeout (Redis unavailable or slow)");
                // User was still created successfully in database
            }
            catch (Exception ex)
            {
                // Other subscription errors - log but don't fail the mutation
                logger.LogWarning(ex, "Failed to send subscription events, but user was created");
                // User was still created successfully in database
            }

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    // Update user
    [Authorize]
    public async Task<User?> UpdateUser(
        int id,
        string? name,
        string? email,
        [Service] ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
        [Service] ILogger<Mutation> logger,
        CancellationToken cancellationToken)
    {
        try
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

            // Invalidate cache with error handling
            try
            {
                logger.LogInformation("Invalidating cache for user {Id}", id);
                var db = redis.GetDatabase();
                
                // Use timeout to avoid hanging
                await db.KeyDeleteAsync($"user:{id}");
                await db.KeyDeleteAsync($"user:email:{email}");
                
                logger.LogInformation("Cache invalidated");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to invalidate Redis cache");
                // Don't fail the mutation if cache invalidation fails
            }

            // Trigger subscription with timeout
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                await eventSender.SendAsync(
                    nameof(Subscription.OnUserUpdated), 
                    user, 
                    timeoutCts.Token);
                
                await eventSender.SendAsync(
                    nameof(Subscription.OnUserChanged), 
                    user, 
                    timeoutCts.Token);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Subscription event timeout during update");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send subscription events during update");
            }

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user");
            throw;
        }
    }

    // Delete user
    [Authorize]
    public async Task<bool> DeleteUser(
        int id,
        [Service] ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        [Service] IConnectionMultiplexer redis,
        [Service] ILogger<Mutation> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await context.Users.FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return false;
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync(cancellationToken);

            // Invalidate cache with error handling
            try
            {
                logger.LogInformation("Invalidating cache for deleted user {Id}", id);
                var db = redis.GetDatabase();
                
                await db.KeyDeleteAsync($"user:{id}");
                await db.KeyDeleteAsync($"user:email:{user.Email}");
                
                logger.LogInformation("Cache invalidated");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to invalidate Redis cache");
            }

            // Trigger subscription with timeout
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                await eventSender.SendAsync(
                    nameof(Subscription.OnUserDeleted), 
                    user, 
                    timeoutCts.Token);
                
                await eventSender.SendAsync(
                    nameof(Subscription.OnUserChanged), 
                    user, 
                    timeoutCts.Token);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Subscription event timeout during delete");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send subscription events during delete");
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user");
            throw;
        }
    }

     // Add this method to your existing Mutation class
        public async Task<string> PublishUserEvent(
            [Service] IKafkaService kafkaService,
            string userId,
            string eventType,
            string eventData)
        {
            var kafkaMessage = new
            {
                userId,
                eventType,
                eventData,
                timestamp = DateTime.UtcNow
            };

            var messageJson = System.Text.Json.JsonSerializer.Serialize(kafkaMessage);
            
            var success = await kafkaService.ProduceMessageAsync(
                "user-events",
                userId,
                messageJson
            );

            return success 
                ? "Event published successfully" 
                : "Failed to publish event";
        }
}