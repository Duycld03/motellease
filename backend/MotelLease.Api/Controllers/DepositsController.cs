using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Deposits;
using MotelLease.Application.Deposits.Contracts;
using MotelLease.Application.Leases;
using MotelLease.Application.Leases.Contracts;
using MotelLease.Application.Payments;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Deposit requests: a tenant asks to hold a room, the owner or assigned staff answer, and an
/// accepted request holds the room until its payment deadline (docs/api-design.md).
///
/// Checkout opens a payment attempt only. Money state moves in one place and it is not here — see
/// <see cref="PaymentsController"/> for the IPN callback that confirms it.
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

    /// <summary>
    /// Opens a payment attempt and returns the gateway URL. Nothing is paid by calling this: the
    /// deposit only becomes Paid when the IPN callback confirms it (docs/domain-rules.md §9.8).
    /// </summary>
    [HttpPost("{id:guid}/checkout")]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(PaymentCheckoutResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentCheckoutResponse>> Checkout(
        Guid id,
        StartPaymentRequest request,
        [FromServices] StartDepositPaymentHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(
            id,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            cancellationToken));

    /// <summary>
    /// Turns a paid deposit into the lease it was paying for. Answered by the owner or assigned
    /// staff, because signing the contract is their side of the transaction.
    /// </summary>
    [HttpPost("{id:guid}/confirm-lease")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(LeaseResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LeaseResponse>> ConfirmLease(
        Guid id,
        [FromServices] ConfirmDepositLeaseHandler handler,
        CancellationToken cancellationToken)
    {
        var lease = await handler.HandleAsync(id, cancellationToken);

        return Created($"/api/v1/leases/{lease.Id}", lease);
    }
}
