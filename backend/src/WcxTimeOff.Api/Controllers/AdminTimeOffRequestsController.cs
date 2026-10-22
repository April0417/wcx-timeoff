using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WcxTimeOff.Api.Auth;
using WcxTimeOff.Api.Contracts;
using WcxTimeOff.Api.Domain;
using WcxTimeOff.Api.Services;

namespace WcxTimeOff.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/timeoff-requests")]
public sealed class AdminTimeOffRequestsController(ITimeOffService timeOff) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TimeOffRequestDto>>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals(TimeOffStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "unsupported_status",
                Message = "This queue currently supports status=Pending only."
            });
        }

        return Ok(await timeOff.ListPendingAsync(cancellationToken));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<TimeOffRequestDto>> Approve(string id, CancellationToken cancellationToken)
    {
        return Ok(await timeOff.ApproveAsync(id, CurrentUser.GetUserId(User), cancellationToken));
    }

    [HttpPost("{id}/deny")]
    public async Task<ActionResult<TimeOffRequestDto>> Deny(string id, CancellationToken cancellationToken)
    {
        return Ok(await timeOff.DenyAsync(id, CurrentUser.GetUserId(User), cancellationToken));
    }
}
