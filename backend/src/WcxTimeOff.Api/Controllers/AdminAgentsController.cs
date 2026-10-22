using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/agents")]
public sealed class AdminAgentsController(ICreditService credits) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentOverviewDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await credits.ListAgentsAsync(cancellationToken));
    }

    [HttpGet("{id}/credits")]
    public async Task<ActionResult<AgentOverviewDto>> GetCredits(string id, CancellationToken cancellationToken)
    {
        return Ok(await credits.GetAgentCreditsAsync(id, cancellationToken));
    }

    [HttpPut("{id}/credits")]
    public async Task<ActionResult<AgentOverviewDto>> UpdateCredits(
        string id,
        [FromBody] UpdateEarnedCreditsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EarnedCredits is null)
        {
            return BadRequest(new ErrorResponse { Error = "invalid_credits", Message = "Earned credits are required." });
        }

        return Ok(await credits.UpdateEarnedCreditsAsync(id, request.EarnedCredits.Value, cancellationToken));
    }
}
