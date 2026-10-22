using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Errors;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Invalid_credentials_are_rejected()
    {
        var users = new InMemoryUsers();
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = "admin-1", Name = "Admin", Email = "admin@wcx.local", Role = UserRole.Admin };
        user.PasswordHash = hasher.HashPassword(user, "Admin123!");
        users.Add(user);
        var auth = new AuthService(users, new StubTokens(), hasher);

        var act = () => auth.LoginAsync(new LoginRequest { Email = "admin@wcx.local", Password = "wrong" }, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(x => x.Error == "invalid_credentials");
    }

    private sealed class StubTokens : IJwtTokenService
    {
        public string CreateToken(User user) => "token";
    }
}
