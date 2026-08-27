using MotelLease.Domain.Enums;

namespace MotelLease.Application.Reports.Contracts;

public sealed record CreateReportRequest(
    ReportTargetType TargetType,
    Guid TargetId,
    string Reason,
    string? Details = null);

public sealed record ResolveReportRequest(
    string? Resolution = null);

public sealed record DismissReportRequest(
    string? Resolution = null);

public sealed record ReportResponse(
    Guid Id,
    Guid ReporterUserId,
    string ReporterFullName,
    ReportTargetType TargetType,
    Guid TargetId,
    string Reason,
    string? Details,
    ReportStatus Status,
    Guid? ProcessedByUserId,
    string? ProcessedByFullName,
    DateTimeOffset? ProcessedAt,
    string? Resolution,
    DateTimeOffset CreatedAt);
