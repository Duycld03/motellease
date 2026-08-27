using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Catalogue;
using MotelLease.Application.Catalogue.Contracts;
using MotelLease.Application.Common.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/boarding-houses")]
[AllowAnonymous]
public sealed class PublicBoardingHousesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PublicBoardingHouseCardResponse>>> Search(
        [FromQuery] BoardingHouseSearchFilter filter,
        [FromServices] SearchBoardingHousesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IReadOnlyList<BoardingHouseNearbyResponse>>> Nearby(
        [FromQuery] BoardingHouseNearbyRequest request,
        [FromServices] GetNearbyBoardingHousesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("map")]
    public async Task<ActionResult<IReadOnlyList<BoardingHouseMapMarkerResponse>>> Map(
        [FromQuery] BoardingHouseMapRequest request,
        [FromServices] GetMapBoardingHousesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicBoardingHouseDetailResponse>> GetById(
        Guid id,
        [FromServices] GetBoardingHouseDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/rooms")]
    public async Task<ActionResult<IReadOnlyList<PublicVacantRoomResponse>>> GetVacantRooms(
        Guid id,
        [FromServices] GetBoardingHouseVacantRoomsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<ActionResult<PagedResponse<PublicReviewResponse>>> GetReviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromServices] GetBoardingHouseReviewsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
