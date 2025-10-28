using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using backend.Models;

public class Subscription
{
    // Subscribe to user creation events
    [Subscribe]
    [Topic(nameof(OnUserCreated))]
    public User OnUserCreated([EventMessage] User user) => user;

    // Subscribe to user update events
    [Subscribe]
    [Topic(nameof(OnUserUpdated))]
    public User OnUserUpdated([EventMessage] User user) => user;

    // Subscribe to user deletion events
    [Subscribe]
    [Topic(nameof(OnUserDeleted))]
    public User OnUserDeleted([EventMessage] User user) => user;

    // Subscribe to any user change (create, update, or delete)
    [Subscribe]
    [Topic(nameof(OnUserChanged))]
    public User OnUserChanged([EventMessage] User user) => user;
}