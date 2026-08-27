using MotelLease.Domain.Enums;

namespace MotelLease.Application.Tasks.Contracts;

public sealed record CreateTaskRequest(
    Guid BoardingHouseId,
    Guid AssignedToUserId,
    string Title,
    string? Details = null,
    TaskPriority Priority = TaskPriority.Medium,
    DateOnly? DueDate = null);

public sealed record UpdateTaskRequest(
    Guid AssignedToUserId,
    string Title,
    string? Details = null,
    TaskPriority Priority = TaskPriority.Medium,
    DateOnly? DueDate = null);

public sealed record UpdateTaskStatusRequest(
    WorkTaskStatus Status);

public sealed record TaskResponse(
    Guid Id,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid CreatedByUserId,
    Guid AssignedToUserId,
    string AssignedToFullName,
    Guid? MaintenanceRequestId,
    string Title,
    string? Details,
    TaskPriority Priority,
    WorkTaskStatus Status,
    DateOnly? DueDate,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);
