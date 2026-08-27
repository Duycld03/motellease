using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Security;
using MotelLease.Application.RoomTypes;
using MotelLease.Application.RoomTypes.Contracts;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Room types of one property: the price, size, occupancy cap and facilities that its rooms
/// inherit. A signed lease keeps its own copy of the rent, so editing a price here never changes
/// an existing contract (docs/domain-rules.md §3.2).
/// </summary>
[ApiController]
[Route("api/v1/my/boarding-houses/{boardingHouseId:guid}/room-types")]
[Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
public sealed class MyRoomTypesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoomTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoomTypeResponse>>> List(
        Guid boardingHouseId,
        [FromServices] ListRoomTypesHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(boardingHouseId, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(RoomTypeResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoomTypeResponse>> Create(
        Guid boardingHouseId,
        SaveRoomTypeRequest request,
        [FromServices] CreateRoomTypeHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(boardingHouseId, request, cancellationToken);

        return CreatedAtAction(nameof(List), new { boardingHouseId }, created);
    }

    [HttpPut("{typeId:guid}")]
    [ProducesResponseType(typeof(RoomTypeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoomTypeResponse>> Update(
        Guid boardingHouseId,
        Guid typeId,
        SaveRoomTypeRequest request,
        [FromServices] UpdateRoomTypeHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(boardingHouseId, typeId, request, cancellationToken));

    [HttpDelete("{typeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid boardingHouseId,
        Guid typeId,
        [FromServices] DeleteRoomTypeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(boardingHouseId, typeId, cancellationToken);

        return NoContent();
    }
}
