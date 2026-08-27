using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Appointments.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Appointments;

internal static class AppointmentRules
{
    internal static IQueryable<AppointmentResponse> Project(IQueryable<Appointment> query) =>
        query.Select(a => new AppointmentResponse(
            a.Id,
            a.RoomId,
            a.Room.RoomNumber,
            a.Room.BoardingHouseId,
            a.Room.BoardingHouse.Name,
            a.UserId,
            a.User.FullName,
            a.User.PhoneNumber,
            a.AppointmentDate,
            a.Status,
            a.Note,
            a.ReasonForCancel,
            a.HandledByUserId,
            a.CreatedAt));

    internal static async Task<AppointmentResponse> LoadAsync(
        IAppDbContext database,
        Guid appointmentId,
        CancellationToken cancellationToken) =>
        await Project(database.Appointments.AsNoTracking().Where(a => a.Id == appointmentId))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Appointment.NotFound);

    internal static async Task<Appointment> RequireAsync(
        IAppDbContext database,
        Guid appointmentId,
        CancellationToken cancellationToken) =>
        await database.Appointments.FirstOrDefaultAsync(
            a => a.Id == appointmentId, cancellationToken)
        ?? throw new NotFoundException(MessageKeys.Appointment.NotFound);

    /// <summary>
    /// The property a request belongs to, read without the soft-delete filter: a room may have
    /// been removed from the listing after the visit was booked, and the request still has to
    /// resolve to the property whose owner answers for it.
    /// </summary>
    internal static async Task<Guid> BoardingHouseIdAsync(
        IAppDbContext database,
        Guid roomId,
        CancellationToken cancellationToken) =>
        await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == roomId)
            .Select(r => r.BoardingHouseId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// What the tenant is told when a viewing is answered. The payload is stored, not rendered,
    /// so the sentence is built in the reader's language when they open it (§7).
    /// </summary>
    internal static async Task NotifyTenantAsync(
        IAppDbContext database,
        NotificationDispatcher notifications,
        Appointment appointment,
        CancellationToken cancellationToken)
    {
        var room = await database.Rooms
            .IgnoreQueryFilters()
            .Where(r => r.Id == appointment.RoomId)
            .Select(r => new { r.RoomNumber, HouseName = r.BoardingHouse.Name })
            .FirstAsync(cancellationToken);

        notifications.Queue(
            appointment.UserId,
            NotificationType.AppointmentHandled,
            new
            {
                appointmentId = appointment.Id,
                status = appointment.Status.ToString(),
                roomNumber = room.RoomNumber,
                boardingHouseName = room.HouseName,
                appointmentDate = appointment.AppointmentDate
            },
            linkUrl: $"/appointments/{appointment.Id}");
    }
}

/// <summary>
/// GET /appointments. A tenant sees the visits they booked; an owner or staff member sees the
/// ones on the properties they run. Same endpoint, different rows — the role decides the scope,
/// not the path (docs/api-design.md).
/// </summary>
public sealed class ListAppointmentsHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<AppointmentResponse>> HandleAsync(
        RequestStatus? status,
        Guid? boardingHouseId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var query = currentUser.Role == UserRole.Tenant
            ? database.Appointments.Where(a => a.UserId == userId)
            : database.Appointments.Where(a =>
                access.Managed().Any(b => b.Id == a.Room.BoardingHouseId));

        if (status is { } wanted)
        {
            query = query.Where(a => a.Status == wanted);
        }

        if (boardingHouseId is { } house)
        {
            query = query.Where(a => a.Room.BoardingHouseId == house);
        }

        return await Paged.FromAsync(
            AppointmentRules.Project(
                query.AsNoTracking().OrderByDescending(a => a.AppointmentDate)),
            page,
            pageSize,
            cancellationToken);
    }
}

/// <summary>
/// GET /appointments/{id}. Reachable by the tenant who booked it and by whoever runs the
/// property; anybody else is refused by the boarding house check.
/// </summary>
public sealed class GetAppointmentHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    ICurrentUser currentUser)
{
    public async Task<AppointmentResponse> HandleAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await AppointmentRules.RequireAsync(
            database, appointmentId, cancellationToken);

        if (appointment.UserId != currentUser.RequireUserId())
        {
            await access.RequireStaffOrOwnerAsync(
                await AppointmentRules.BoardingHouseIdAsync(
                    database, appointment.RoomId, cancellationToken),
                cancellationToken);
        }

        return await AppointmentRules.LoadAsync(database, appointment.Id, cancellationToken);
    }
}

/// <summary>
/// POST /appointments. A viewing is only bookable on a vacant room of a published listing: a
/// draft is not public yet, and a room already held or lived in has nothing to show.
/// </summary>
public sealed class BookAppointmentHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<AppointmentResponse> HandleAsync(
        BookAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var now = time.GetUtcNow();

        if (request.AppointmentDate <= now)
        {
            throw new BusinessRuleException(MessageKeys.Appointment.DateInPast);
        }

        var room = await database.Rooms
            .Where(r => r.Id == request.RoomId)
            .Select(r => new { r.Id, r.Status, r.BoardingHouse.ListingStatus })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Room.NotFound);

        if (room.ListingStatus != ListingStatus.Published)
        {
            throw new BusinessRuleException(MessageKeys.Appointment.ListingNotPublished);
        }

        if (room.Status != RoomStatus.Available)
        {
            throw new BusinessRuleException(MessageKeys.Appointment.RoomNotAvailable);
        }

        // One live request per person per room: a second one holds nothing extra and just gives
        // the owner two rows to answer. "Live" means still ahead — a visit whose time has passed
        // blocks nothing, whether or not the sweep has closed it yet.
        var alreadyRequested = await database.Appointments.AnyAsync(
            a => a.RoomId == room.Id
                 && a.UserId == userId
                 && a.AppointmentDate > now
                 && (a.Status == RequestStatus.Pending || a.Status == RequestStatus.Accepted),
            cancellationToken);

        if (alreadyRequested)
        {
            throw new ConflictException(MessageKeys.Appointment.AlreadyRequested);
        }

        var appointment = new Appointment
        {
            UserId = userId,
            RoomId = room.Id,
            AppointmentDate = request.AppointmentDate,
            Status = RequestStatus.Pending,
            Note = request.Note?.Trim()
        };

        database.Appointments.Add(appointment);

        await database.SaveChangesAsync(cancellationToken);

        return await AppointmentRules.LoadAsync(database, appointment.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /appointments/{id}/approve and /reject. Both answer a Pending request, record who answered
/// it, and notify the tenant in the same save as the status change.
/// </summary>
public sealed class AnswerAppointmentHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    NotificationDispatcher notifications,
    ICurrentUser currentUser)
{
    public Task<AppointmentResponse> ApproveAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default) =>
        AnswerAsync(appointmentId, RequestStatus.Accepted, reason: null, cancellationToken);

    public Task<AppointmentResponse> RejectAsync(
        Guid appointmentId,
        RejectAppointmentRequest request,
        CancellationToken cancellationToken = default) =>
        AnswerAsync(
            appointmentId, RequestStatus.Rejected, request.Reason.Trim(), cancellationToken);

    private async Task<AppointmentResponse> AnswerAsync(
        Guid appointmentId,
        RequestStatus answer,
        string? reason,
        CancellationToken cancellationToken)
    {
        var appointment = await AppointmentRules.RequireAsync(
            database, appointmentId, cancellationToken);

        await access.RequireStaffOrOwnerAsync(
            await AppointmentRules.BoardingHouseIdAsync(
                database, appointment.RoomId, cancellationToken),
            cancellationToken);

        if (appointment.Status != RequestStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Appointment.NotPending);
        }

        appointment.Status = answer;
        appointment.HandledByUserId = currentUser.RequireUserId();

        // The only free-text field on the row. A rejection reason and a cancellation reason are
        // the same thing from the tenant's side: why the visit is not happening.
        if (reason is not null)
        {
            appointment.ReasonForCancel = reason;
        }

        await AppointmentRules.NotifyTenantAsync(
            database, notifications, appointment, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);

        await notifications.DeliverAsync(cancellationToken);

        return await AppointmentRules.LoadAsync(database, appointment.Id, cancellationToken);
    }
}

/// <summary>
/// PUT /appointments/{id}/cancel. The tenant's own withdrawal, allowed while the visit is still
/// ahead of them: pending or already accepted.
/// </summary>
public sealed class CancelAppointmentHandler(IAppDbContext database, ICurrentUser currentUser)
{
    public async Task<AppointmentResponse> HandleAsync(
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var appointment = await AppointmentRules.RequireAsync(
            database, appointmentId, cancellationToken);

        if (appointment.UserId != currentUser.RequireUserId())
        {
            throw new ForbiddenException(MessageKeys.Appointment.NotYours);
        }

        if (appointment.Status is not (RequestStatus.Pending or RequestStatus.Accepted))
        {
            throw new BusinessRuleException(MessageKeys.Appointment.NotCancellable);
        }

        appointment.Status = RequestStatus.Cancelled;
        appointment.ReasonForCancel = request.Reason?.Trim();

        await database.SaveChangesAsync(cancellationToken);

        return await AppointmentRules.LoadAsync(database, appointment.Id, cancellationToken);
    }
}
