using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Catalogue;
using MotelLease.Application.RoomTypes.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/facilities")]
[AllowAnonymous]
public sealed class FacilitiesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FacilityResponse>>> List(
        [FromServices] ListFacilitiesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
