using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Maintenance.Contracts;

public sealed record CreateMaintenanceRequest(
    Guid LeaseId,
    MaintenanceCategory Category,
    string Description,
    IReadOnlyList<string>? ImageUrls = null);

public sealed record AcceptMaintenanceRequest(
    Guid? AssignToStaffUserId = null,
    string? TaskTitle = null,
    DateOnly? DueDate = null);

public sealed record RejectMaintenanceRequest(
    string? Reason = null);

public sealed record MaintenanceRequestResponse(
    Guid Id,
    Guid LeaseId,
    Guid RoomId,
    string RoomNumber,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid ReportedByUserId,
    string ReporterFullName,
    MaintenanceCategory Category,
    string Description,
    MaintenanceStatus Status,
    Guid? TaskId,
    IReadOnlyList<ImageResponse> Images,
    DateTimeOffset CreatedAt);
