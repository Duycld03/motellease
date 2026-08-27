using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Staff;
using MotelLease.Application.Staff.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class StaffController : ControllerBase
{
    [HttpGet("my/staff")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<IReadOnlyList<StaffSummaryResponse>>> ListStaff(
        [FromServices] ListStaffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("my/staff")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<StaffDetailResponse>> CreateStaff(
        [FromBody] CreateStaffRequest request,
        [FromServices] CreateStaffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStaff), new { id = result.Id }, result);
    }

    [HttpGet("my/staff/{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<StaffDetailResponse>> GetStaff(
        Guid id,
        [FromServices] GetStaffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("my/staff/{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<StaffDetailResponse>> UpdateStaff(
        Guid id,
        [FromBody] UpdateStaffRequest request,
        [FromServices] UpdateStaffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("my/staff/{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> LockStaff(
        Guid id,
        [FromServices] LockStaffHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("my/boarding-houses/{id:guid}/staff")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<IReadOnlyList<StaffAssignmentResponse>>> ListHouseStaff(
        Guid id,
        [FromServices] ListBoardingHouseStaffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("my/boarding-houses/{id:guid}/staff")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<StaffAssignmentResponse>> AssignStaff(
        Guid id,
        [FromBody] AssignStaffRequest request,
        [FromServices] AssignStaffHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("my/boarding-houses/{id:guid}/staff/{staffId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> UnassignStaff(
        Guid id,
        Guid staffId,
        [FromServices] UnassignStaffHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, staffId, cancellationToken);
        return NoContent();
    }
}
