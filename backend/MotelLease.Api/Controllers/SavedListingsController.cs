using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.SavedListings;
using MotelLease.Application.SavedListings.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/me/saved-listings")]
[Authorize]
public sealed class SavedListingsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SavedListingResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListSavedListingsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SavedListingResponse>> Save(
        [FromBody] SaveListingRequest request,
        [FromServices] SaveListingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{boardingHouseId:guid}")]
    public async Task<IActionResult> Remove(
        Guid boardingHouseId,
        [FromServices] RemoveSavedListingHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(boardingHouseId, cancellationToken);
        return NoContent();
    }
}
