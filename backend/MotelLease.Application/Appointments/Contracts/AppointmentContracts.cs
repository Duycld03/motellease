using MotelLease.Domain.Enums;

namespace MotelLease.Application.Appointments.Contracts;

/// <summary>
/// One viewing request. The tenant's name and phone number are included because the point of the
/// list, for an owner, is to know who is coming and how to reach them.
/// </summary>
public sealed record AppointmentResponse(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid TenantUserId,
    string TenantFullName,
    string? TenantPhoneNumber,
    DateTimeOffset AppointmentDate,
    RequestStatus Status,
    string? Note,
    string? ReasonForCancel,
    Guid? HandledByUserId,
    DateTimeOffset CreatedAt);

public sealed record BookAppointmentRequest(
    Guid RoomId,
    DateTimeOffset AppointmentDate,
    string? Note);

public sealed record RejectAppointmentRequest(string Reason);

public sealed record CancelAppointmentRequest(string? Reason);
