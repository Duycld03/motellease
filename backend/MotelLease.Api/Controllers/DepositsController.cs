using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Deposits;
using MotelLease.Application.Deposits.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Deposit requests: a tenant asks to hold a room, the owner or assigned staff answer, and an
/// accepted request holds the room until its payment deadline (docs/api-design.md).
///
/// Paying for one is not here. Money state may only change through a verified server-to-server
/// callback (CLAUDE.md, Hard prohibitions), so checkout and the lease that follows a paid deposit
/// ship with the payment group.
/// </summary>
[ApiController]
[Route("api/v1/deposits")]
[Authorize]
public sealed class DepositsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DepositResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DepositResponse>>> List(
        [FromServices] ListDepositsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] DepositStatus? status = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(
            status, boardingHouseId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepositResponse>> Get(
        Guid id,
        [FromServices] GetDepositHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DepositResponse>> Create(
        RequestDepositRequest request,
        [FromServices] RequestDepositHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepositResponse>> Approve(
        Guid id,
        [FromServices] AnswerDepositHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.ApproveAsync(id, cancellationToken));

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepositResponse>> Reject(
        Guid id,
        RejectDepositRequest request,
        [FromServices] AnswerDepositHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.RejectAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepositResponse>> Cancel(
        Guid id,
        CancelDepositRequest request,
        [FromServices] CancelDepositHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));

    [HttpGet("{id:guid}/contract-preview")]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(DepositContractPreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepositContractPreviewResponse>> ContractPreview(
        Guid id,
        [FromServices] PreviewDepositContractHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));
}
