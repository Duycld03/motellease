using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Admin;
using MotelLease.Application.Admin.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/admin/stats")]
[Authorize(Roles = "Admin")]
public sealed class AdminStatsController : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminPlatformStatsResponse>> GetSummary(
        [FromServices] AdminGetStatsSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
