using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Bills;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/bills")]
[Authorize]
public sealed class BillsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BillResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BillResponse>>> List(
        [FromServices] ListBillsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] BillStatus? status = null,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] Guid? roomId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(status, month, year, boardingHouseId, roomId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillResponse>> Get(
        Guid id,
        [FromServices] GetBillHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] GenerateBillPdfHandler handler,
        CancellationToken cancellationToken)
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var result = await handler.HandleAsync(id, language, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("preview")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillResponse>> Preview(
        PreviewBillRequest request,
        [FromServices] PreviewBillHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillResponse>> Create(
        CreateBillRequest request,
        [FromServices] CreateBillHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillResponse>> UpdateDraft(
        Guid id,
        UpdateDraftBillRequest request,
        [FromServices] UpdateDraftBillHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/issue")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillResponse>> Issue(
        Guid id,
        IssueDraftBillRequest request,
        [FromServices] IssueDraftBillHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillResponse>> Cancel(
        Guid id,
        [FromServices] CancelBillHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));
}
