using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Withdrawals;
using MotelLease.Application.Withdrawals.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/withdraw-requests")]
public sealed class WithdrawRequestsController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<PagedResponse<WithdrawRequestResponse>>> List(
        [FromQuery] RequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListWithdrawRequestsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<WithdrawRequestResponse>> Create(
        [FromBody] CreateWithdrawRequest request,
        [FromServices] CreateWithdrawRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<WithdrawRequestResponse>> GetById(
        Guid id,
        [FromServices] GetWithdrawRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WithdrawRequestResponse>> Approve(
        Guid id,
        [FromServices] ApproveWithdrawRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WithdrawRequestResponse>> Reject(
        Guid id,
        [FromBody] RejectWithdrawRequest request,
        [FromServices] RejectWithdrawRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
