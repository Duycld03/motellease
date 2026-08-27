using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Extensions;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/extension-requests")]
[Authorize]
public sealed class ExtensionRequestsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ExtensionRequestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ExtensionRequestResponse>>> List(
        [FromServices] ListExtensionRequestsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(status, boardingHouseId, page, pageSize, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(ExtensionRequestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExtensionRequestResponse>> Create(
        CreateExtensionRequest request,
        [FromServices] CreateExtensionRequestHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExtensionRequestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExtensionRequestResponse>> Get(
        Guid id,
        [FromServices] GetExtensionRequestHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(ExtensionRequestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExtensionRequestResponse>> Approve(
        Guid id,
        [FromServices] ApproveExtensionRequestHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(ExtensionRequestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExtensionRequestResponse>> Reject(
        Guid id,
        RejectExtensionRequest request,
        [FromServices] RejectExtensionRequestHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));
}
