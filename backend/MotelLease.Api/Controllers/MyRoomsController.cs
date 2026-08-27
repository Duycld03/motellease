using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Rooms;
using MotelLease.Application.Rooms.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Rooms of one property. Two shapes of route on purpose: the collection hangs off the boarding
/// house, while the day-to-day actions on a single room (taking it out of service, recording a
/// meter) address the room directly and read its property from the row.
/// </summary>
[ApiController]
[Route("api/v1/my")]
[Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
public sealed class MyRoomsController : ControllerBase
{
    [HttpGet("boarding-houses/{boardingHouseId:guid}/rooms")]
    [ProducesResponseType(typeof(IReadOnlyList<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomResponse>>> List(
        Guid boardingHouseId,
        [FromServices] ListRoomsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] RoomStatus? status = null) =>
        Ok(await handler.HandleAsync(boardingHouseId, status, cancellationToken));

    [HttpPost("boarding-houses/{boardingHouseId:guid}/rooms")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoomResponse>> Create(
        Guid boardingHouseId,
        SaveRoomRequest request,
        [FromServices] CreateRoomHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(boardingHouseId, request, cancellationToken);

        return CreatedAtAction(nameof(List), new { boardingHouseId }, created);
    }

    [HttpPut("boarding-houses/{boardingHouseId:guid}/rooms/{roomId:guid}")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomResponse>> Update(
        Guid boardingHouseId,
        Guid roomId,
        SaveRoomRequest request,
        [FromServices] UpdateRoomHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(boardingHouseId, roomId, request, cancellationToken));

    [HttpDelete("boarding-houses/{boardingHouseId:guid}/rooms/{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid boardingHouseId,
        Guid roomId,
        [FromServices] DeleteRoomHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(boardingHouseId, roomId, cancellationToken);

        return NoContent();
    }

    /// <summary>Maintenance ⇄ Available. The other two values are derived, not chosen.</summary>
    [HttpPut("rooms/{roomId:guid}/status")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomResponse>> UpdateStatus(
        Guid roomId,
        UpdateRoomStatusRequest request,
        [FromServices] UpdateRoomStatusHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(roomId, request, cancellationToken));

    /// <summary>
    /// The current meter figures, which next month's bill reads as its opening values.
    /// </summary>
    [HttpPut("rooms/{roomId:guid}/meter-readings")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomResponse>> UpdateMeterReadings(
        Guid roomId,
        UpdateMeterReadingsRequest request,
        [FromServices] UpdateMeterReadingsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(roomId, request, cancellationToken));
}
