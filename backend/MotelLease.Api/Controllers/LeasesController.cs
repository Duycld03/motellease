using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Bills;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Leases;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/leases")]
[Authorize]
public sealed class LeasesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<LeaseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<LeaseResponse>>> List(
        [FromServices] ListLeasesHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] LeaseStatus? status = null,
        [FromQuery] Guid? roomId = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(status, roomId, boardingHouseId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaseResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseResponse>> Get(
        Guid id,
        [FromServices] GetLeaseHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpGet("{id:guid}/bills")]
    [ProducesResponseType(typeof(PagedResponse<BillResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BillResponse>>> GetBills(
        Guid id,
        [FromServices] ListBillsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(page: page, pageSize: pageSize, cancellationToken: cancellationToken));

    [HttpPost("{id:guid}/tenants")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(LeaseResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseResponse>> AddTenant(
        Guid id,
        AddLeaseTenantRequest request,
        [FromServices] AddLeaseTenantHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}/tenants/{tenantId:guid}")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(LeaseResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseResponse>> RemoveTenant(
        Guid id,
        Guid tenantId,
        [FromServices] RemoveLeaseTenantHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, tenantId, cancellationToken));

    [HttpGet("{id:guid}/termination-preview")]
    [ProducesResponseType(typeof(LeaseTerminationPreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseTerminationPreviewResponse>> PreviewTermination(
        Guid id,
        [FromQuery] decimal finalElectricityReading,
        [FromQuery] decimal finalWaterReading,
        [FromQuery] decimal depositDeducted,
        [FromServices] PreviewLeaseTerminationHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, finalElectricityReading, finalWaterReading, depositDeducted, cancellationToken));

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(LeaseResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseResponse>> Terminate(
        Guid id,
        TerminateLeaseRequest request,
        [FromServices] TerminateLeaseHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));
}
