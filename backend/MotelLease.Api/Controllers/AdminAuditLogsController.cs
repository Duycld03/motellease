using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Admin;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Common.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class AdminAuditLogsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> List(
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] AdminListAuditLogsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(actorUserId, entityType, entityId, from, to, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
