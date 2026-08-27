using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Admin;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Common.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/admin/reviews")]
[Authorize(Roles = "Admin")]
public sealed class AdminReviewsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminReviewResponse>>> List(
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] AdminListReviewsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(boardingHouseId, search, includeDeleted, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AdminDeleteReviewHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromServices] AdminRestoreReviewHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
