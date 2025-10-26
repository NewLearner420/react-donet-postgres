using backend.Data;
using backend.Models;
using HotChocolate.Data;

namespace backend.GraphQL;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([Service] ApplicationDbContext context)
        => context.Users;

    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetUser([Service] ApplicationDbContext context, int id)
        => context.Users.Where(u => u.Id == id);
}