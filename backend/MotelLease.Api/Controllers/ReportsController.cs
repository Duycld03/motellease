using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Reports;
using MotelLease.Application.Reports.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReportsController : ControllerBase
{
    [HttpPost("reports")]
    [Authorize]
    public async Task<ActionResult<ReportResponse>> Create(
        [FromBody] CreateReportRequest request,
        [FromServices] CreateReportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("me/reports")]
    [Authorize]
    public async Task<ActionResult<PagedResponse<ReportResponse>>> GetMyReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListUserReportsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("reports")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<ReportResponse>>> ListAdmin(
        [FromQuery] ReportTargetType? targetType = null,
        [FromQuery] ReportStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListAdminReportsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(targetType, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("reports/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportResponse>> GetById(
        Guid id,
        [FromServices] GetAdminReportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("reports/{id:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportResponse>> Resolve(
        Guid id,
        [FromBody] ResolveReportRequest request,
        [FromServices] ResolveReportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("reports/{id:guid}/dismiss")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportResponse>> Dismiss(
        Guid id,
        [FromBody] DismissReportRequest request,
        [FromServices] DismissReportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
