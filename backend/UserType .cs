using backend.Models;
using HotChocolate.Types;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Description("Represents a user in the system");

        descriptor
            .Field(u => u.Id)
            .Description("The unique identifier of the user");

        descriptor
            .Field(u => u.Name)
            .Description("The name of the user");

        descriptor
            .Field(u => u.Email)
            .Description("The email address of the user");

        descriptor
            .Field(u => u.CreatedAt)
            .Description("The timestamp when the user was created");

        descriptor
            .Field(u => u.UpdatedAt)
            .Description("The timestamp when the user was last updated");
    }
}