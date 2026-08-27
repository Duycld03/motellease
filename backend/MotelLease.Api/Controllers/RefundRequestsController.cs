using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Refunds;
using MotelLease.Application.Refunds.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/refund-requests")]
public sealed class RefundRequestsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PagedResponse<RefundRequestResponse>>> List(
        [FromQuery] RequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListRefundRequestsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<RefundRequestResponse>> Create(
        [FromBody] CreateRefundRequest request,
        [FromServices] CreateRefundRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<RefundRequestResponse>> GetById(
        Guid id,
        [FromServices] GetRefundRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<RefundRequestResponse>> Approve(
        Guid id,
        [FromServices] ApproveRefundRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<RefundRequestResponse>> Reject(
        Guid id,
        [FromBody] RejectRefundRequest request,
        [FromServices] RejectRefundRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
