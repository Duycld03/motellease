using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Admin;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/admin/boarding-houses")]
[Authorize(Roles = "Admin")]
public sealed class AdminBoardingHousesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminBoardingHouseResponse>>> List(
        [FromQuery] ListingStatus? listingStatus = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] AdminListBoardingHousesHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(listingStatus, search, includeDeleted, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/approve")]
    public async Task<ActionResult<AdminBoardingHouseResponse>> Approve(
        Guid id,
        [FromServices] AdminApproveBoardingHouseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<ActionResult<AdminBoardingHouseResponse>> Reject(
        Guid id,
        [FromBody] AdminRejectListingRequest request,
        [FromServices] AdminRejectBoardingHouseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AdminDeleteBoardingHouseHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromServices] AdminRestoreBoardingHouseHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
