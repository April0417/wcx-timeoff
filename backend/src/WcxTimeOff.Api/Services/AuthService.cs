using System.Net;
using Microsoft.AspNetCore.Identity;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Data;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Errors;

namespace WcxTimeOff.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken);
}

public sealed class AuthService(
    IUserRepository users,
    IJwtTokenService tokens,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new AppException(HttpStatusCode.Unauthorized, "invalid_credentials", "Invalid email or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new AppException(HttpStatusCode.Unauthorized, "invalid_credentials", "Invalid email or password.");
        }

        return new AuthResponse
        {
            Token = tokens.CreateToken(user),
            User = Map(user)
        };
    }

    public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken)
            ?? throw new AppException(HttpStatusCode.Unauthorized, "unauthorized", "User was not found.");
        return Map(user);
    }

    public static UserDto Map(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role.ToString()
    };
}
