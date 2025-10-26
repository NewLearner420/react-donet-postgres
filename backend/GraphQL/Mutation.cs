using backend.Data;
using backend.Models;

namespace backend.GraphQL;

public class Mutation
{
    public async Task<User> AddUser(
        [Service] ApplicationDbContext context,
        string name,
        string email)
    {
        var user = new User
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> UpdateUser(
        [Service] ApplicationDbContext context,
        int id,
        string? name = null,
        string? email = null)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            throw new GraphQLException($"User with ID {id} not found.");

        if (!string.IsNullOrEmpty(name))
            user.Name = name;

        if (!string.IsNullOrEmpty(email))
            user.Email = email;

        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUser(
        [Service] ApplicationDbContext context,
        int id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            throw new GraphQLException($"User with ID {id} not found.");

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        return true;
    }
}