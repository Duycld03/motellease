using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Payments;
using MotelLease.Application.Payments.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Payments;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Payment transactions and the gateway callbacks (docs/api-design.md).
///
/// The two callback shapes are deliberately not symmetrical. The IPN endpoint is the only place in
/// the system allowed to move money state: it is server-to-server, it verifies a signature made with
/// our own secret, and it answers in the gateway's own acknowledgement format. The return endpoint is
/// a browser landing on a URL the user controls, so it reads the signature only to pick a page and
/// writes nothing at all (CLAUDE.md, Hard prohibitions).
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<PaymentTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PaymentTransactionResponse>>> List(
        [FromServices] ListPaymentsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] PaymentPurpose? purpose = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(
            status, purpose, ownOnly: false, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentTransactionResponse>> Get(
        Guid id,
        [FromServices] GetPaymentHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    /// <summary>
    /// VNPay's IPN. Anonymous because the caller is VNPay, not a signed-in user; the signature over
    /// the query string is the authentication, and an unsigned call is answered with code 97 without
    /// touching a row.
    /// </summary>
    [HttpGet("vnpay/ipn")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GatewayAcknowledgement), StatusCodes.Status200OK)]
    public async Task<ActionResult<GatewayAcknowledgement>> VnPayIpn(
        [FromServices] ConfirmPaymentHandler handler,
        CancellationToken cancellationToken) =>
        // Always 200: the acknowledgement code in the body is what VNPay reads, and an HTTP error
        // would make it retry a callback that was in fact understood and refused.
        Ok(await handler.HandleAsync(PaymentProvider.VNPay, QueryFields(), cancellationToken));

    /// <summary>
    /// Where VNPay sends the payer's browser. Redirects to the frontend with the outcome and writes
    /// nothing — a payment confirmed from here would be a payment anybody could claim by editing the
    /// address bar.
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn(
        [FromServices] ReadPaymentReturnHandler handler,
        [FromServices] IOptions<AppUrlOptions> urls,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            PaymentProvider.VNPay, QueryFields(), cancellationToken);

        var query = QueryString
            .Create("outcome", result.Outcome.ToString())
            .Add("transactionId", result.TransactionId?.ToString() ?? string.Empty)
            .Add("depositId", result.DepositId?.ToString() ?? string.Empty);

        return Redirect($"{urls.Value.WebBaseUrl.TrimEnd('/')}/payments/result{query}");
    }

    private Dictionary<string, string> QueryFields() =>
        Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.Ordinal);
}

/// <summary>
/// GET /me/payments. The tenant's own history, which is the same query narrowed to themselves — a
/// separate path because it is what the account screen asks for.
/// </summary>
[ApiController]
[Route("api/v1/me/payments")]
[Authorize]
public sealed class MyPaymentsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<PaymentTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PaymentTransactionResponse>>> List(
        [FromServices] ListPaymentsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] PaymentPurpose? purpose = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(
            status, purpose, ownOnly: true, page, pageSize, cancellationToken));
}
