using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await auth.LoginAsync(request, cancellationToken));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await auth.GetCurrentUserAsync(CurrentUser.GetUserId(User), cancellationToken));
    }
}
