using MotelLease.Domain.Enums;

namespace MotelLease.Application.Staff.Contracts;

public sealed record CreateStaffRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    DateOnly HireDate);

public sealed record UpdateStaffRequest(
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    DateOnly HireDate);

public sealed record AssignStaffRequest(
    Guid StaffUserId);

public sealed record StaffSummaryResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    bool IsLocked,
    DateOnly HireDate,
    int ActiveAssignmentsCount,
    DateTimeOffset CreatedAt);

public sealed record StaffDetailResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    bool IsLocked,
    DateOnly HireDate,
    IReadOnlyList<StaffAssignmentResponse> Assignments,
    DateTimeOffset CreatedAt);

public sealed record StaffAssignmentResponse(
    Guid Id,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid StaffUserId,
    string StaffFullName,
    DateTimeOffset AssignedAt);
