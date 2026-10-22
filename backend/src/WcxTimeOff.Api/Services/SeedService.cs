using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using WcxTimeOff.Api.Data;
using WcxTimeOff.Api.Domain;

namespace WcxTimeOff.Api.Services;

public sealed class SeedService(MongoContext context, IPasswordHasher<User> passwordHasher, ILogger<SeedService> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await context.EnsureIndexesAsync(cancellationToken);

        if (await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty, cancellationToken: cancellationToken) > 0)
        {
            return;
        }

        var admin = CreateUser("Avery Admin", "admin@wcx.local", "Admin123!", UserRole.Admin);
        var jordan = CreateUser("Jordan Lee", "jordan@wcx.local", "Agent123!", UserRole.Agent);
        var sam = CreateUser("Sam Patel", "sam@wcx.local", "Agent123!", UserRole.Agent);
        var riley = CreateUser("Riley Chen", "riley@wcx.local", "Agent123!", UserRole.Agent);

        await context.Users.InsertManyAsync([admin, jordan, sam, riley], cancellationToken: cancellationToken);

        await context.CreditBalances.InsertManyAsync(
            [
                new CreditBalance { AgentId = jordan.Id, EarnedCredits = 80, TakenCredits = 0 },
                new CreditBalance { AgentId = sam.Id, EarnedCredits = 40, TakenCredits = 0 },
                new CreditBalance { AgentId = riley.Id, EarnedCredits = 16, TakenCredits = 0 }
            ],
            cancellationToken: cancellationToken);

        logger.LogInformation("Development seed complete: 1 admin and 3 agents.");
    }

    private User CreateUser(string name, string email, string password, UserRole role)
    {
        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Email = email,
            Role = role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }
}
