using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Controllers;

[ApiController]
[Authorize(Roles = "Agent")]
[Route("api/me")]
public sealed class MeController(ICreditService credits) : ControllerBase
{
    [HttpGet("credits")]
    public async Task<ActionResult<AgentOverviewDto>> Credits(CancellationToken cancellationToken)
    {
        return Ok(await credits.GetAgentCreditsAsync(CurrentUser.GetUserId(User), cancellationToken));
    }
}
