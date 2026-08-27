using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Admin;
using MotelLease.Application.Admin.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/admin/facilities")]
[Authorize(Roles = "Admin")]
public sealed class AdminFacilitiesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FacilityDetailResponse>>> List(
        [FromServices] AdminListFacilitiesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<FacilityDetailResponse>> Create(
        [FromBody] CreateFacilityRequest request,
        [FromServices] AdminCreateFacilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FacilityDetailResponse>> GetById(
        Guid id,
        [FromServices] AdminGetFacilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FacilityDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateFacilityRequest request,
        [FromServices] AdminUpdateFacilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AdminDeleteFacilityHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
