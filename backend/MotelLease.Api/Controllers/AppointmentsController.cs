using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Appointments;
using MotelLease.Application.Appointments.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Viewing appointments. Reading is open to everyone involved and the handler decides which rows
/// that means; the write actions are split by who they belong to — a tenant books and cancels, the
/// owner or assigned staff answer (docs/api-design.md).
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public sealed class AppointmentsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> List(
        [FromServices] ListAppointmentsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(
            status, boardingHouseId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentResponse>> Get(
        Guid id,
        [FromServices] GetAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AppointmentResponse>> Book(
        BookAppointmentRequest request,
        [FromServices] BookAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        var booked = await handler.HandleAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = booked.Id }, booked);
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentResponse>> Approve(
        Guid id,
        [FromServices] AnswerAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.ApproveAsync(id, cancellationToken));

    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentResponse>> Reject(
        Guid id,
        RejectAppointmentRequest request,
        [FromServices] AnswerAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.RejectAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.RequireTenant)]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentResponse>> Cancel(
        Guid id,
        CancelAppointmentRequest request,
        [FromServices] CancelAppointmentHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));
}
