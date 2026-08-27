using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Maintenance;
using MotelLease.Application.Maintenance.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/maintenance-requests")]
public sealed class MaintenanceRequestsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PagedResponse<MaintenanceRequestResponse>>> List(
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] MaintenanceStatus? status = null,
        [FromQuery] MaintenanceCategory? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListMaintenanceRequestsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(boardingHouseId, status, category, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<MaintenanceRequestResponse>> Create(
        [FromBody] CreateMaintenanceRequest request,
        [FromServices] CreateMaintenanceRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<MaintenanceRequestResponse>> GetById(
        Guid id,
        [FromServices] GetMaintenanceRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/accept")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<MaintenanceRequestResponse>> Accept(
        Guid id,
        [FromBody] AcceptMaintenanceRequest request,
        [FromServices] AcceptMaintenanceRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/resolve")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<MaintenanceRequestResponse>> Resolve(
        Guid id,
        [FromServices] ResolveMaintenanceRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<MaintenanceRequestResponse>> Reject(
        Guid id,
        [FromBody] RejectMaintenanceRequest request,
        [FromServices] RejectMaintenanceRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
