using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/timeoff-requests")]
public sealed class TimeOffRequestsController(ITimeOffService timeOff) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Agent")]
    public async Task<ActionResult<TimeOffRequestDto>> Create([FromBody] CreateTimeOffRequest request, CancellationToken cancellationToken)
    {
        var created = await timeOff.CreateAsync(CurrentUser.GetUserId(User), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Agent")]
    public async Task<ActionResult<IReadOnlyList<TimeOffRequestDto>>> Mine(CancellationToken cancellationToken)
    {
        return Ok(await timeOff.ListMineAsync(CurrentUser.GetUserId(User), cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TimeOffRequestDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        return Ok(await timeOff.GetByIdAsync(id, CurrentUser.GetUserId(User), isAdmin, cancellationToken));
    }
}
