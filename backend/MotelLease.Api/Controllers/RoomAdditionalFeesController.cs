using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Bills;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Leases;
using MotelLease.Application.Leases.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/rooms/{roomId:guid}")]
[Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
public sealed class RoomAdditionalFeesController : ControllerBase
{
    [HttpGet("additional-fees")]
    [ProducesResponseType(typeof(IReadOnlyList<RoomAdditionalFeeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomAdditionalFeeResponse>>> List(
        Guid roomId,
        [FromServices] ListRoomAdditionalFeesHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null) =>
        Ok(await handler.HandleAsync(roomId, month, year, cancellationToken));

    [HttpPost("additional-fees")]
    [ProducesResponseType(typeof(RoomAdditionalFeeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomAdditionalFeeResponse>> Create(
        Guid roomId,
        CreateRoomAdditionalFeeRequest request,
        [FromServices] CreateRoomAdditionalFeeHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(roomId, request, cancellationToken));

    [HttpPut("additional-fees/{id:guid}")]
    [ProducesResponseType(typeof(RoomAdditionalFeeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomAdditionalFeeResponse>> Update(
        Guid roomId,
        Guid id,
        UpdateRoomAdditionalFeeRequest request,
        [FromServices] UpdateRoomAdditionalFeeHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(roomId, id, request, cancellationToken));

    [HttpDelete("additional-fees/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid roomId,
        Guid id,
        [FromServices] DeleteRoomAdditionalFeeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(roomId, id, cancellationToken);
        return NoContent();
    }

    [HttpGet("lease-history")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeaseResponse>>> LeaseHistory(
        Guid roomId,
        [FromServices] GetRoomLeaseHistoryHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(roomId, cancellationToken));
}
