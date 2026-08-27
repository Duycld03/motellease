using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Reports.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Reports;

public sealed class CreateReportHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<ReportResponse> HandleAsync(
        CreateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var exists = request.TargetType switch
        {
            ReportTargetType.BoardingHouse => await database.BoardingHouses.AnyAsync(b => b.Id == request.TargetId && !b.IsDeleted, cancellationToken),
            ReportTargetType.Review => await database.Reviews.AnyAsync(r => r.Id == request.TargetId && !r.IsDeleted, cancellationToken),
            _ => false
        };

        if (!exists)
        {
            throw new NotFoundException(MessageKeys.Report.TargetNotFound);
        }

        var report = new Report
        {
            ReporterUserId = userId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Reason = request.Reason.Trim(),
            Details = request.Details?.Trim(),
            Status = ReportStatus.Pending
        };

        database.Reports.Add(report);
        await database.SaveChangesAsync(cancellationToken);

        var user = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new ReportResponse(
            report.Id,
            report.ReporterUserId,
            user.FullName,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Details,
            report.Status,
            null,
            null,
            null,
            null,
            report.CreatedAt);
    }
}

public sealed class ListUserReportsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<ReportResponse>> HandleAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var userId = currentUser.RequireUserId();

        var query = database.Reports
            .AsNoTracking()
            .Where(r => r.ReporterUserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportResponse(
                r.Id,
                r.ReporterUserId,
                r.ReporterUser.FullName,
                r.TargetType,
                r.TargetId,
                r.Reason,
                r.Details,
                r.Status,
                r.ProcessedByUserId,
                database.Users.Where(u => u.Id == r.ProcessedByUserId).Select(u => u.FullName).FirstOrDefault(),
                r.ProcessedAt,
                r.Resolution,
                r.CreatedAt));

        return await Paged.FromAsync(query, page, pageSize, cancellationToken);
    }
}

public sealed class ListAdminReportsHandler(IAppDbContext database)
{
    public async Task<PagedResponse<ReportResponse>> HandleAsync(
        ReportTargetType? targetType,
        ReportStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = database.Reports
            .AsNoTracking()
            .Where(r => !r.IsDeleted);

        if (targetType.HasValue)
        {
            query = query.Where(r => r.TargetType == targetType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var projected = query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportResponse(
                r.Id,
                r.ReporterUserId,
                r.ReporterUser.FullName,
                r.TargetType,
                r.TargetId,
                r.Reason,
                r.Details,
                r.Status,
                r.ProcessedByUserId,
                database.Users.Where(u => u.Id == r.ProcessedByUserId).Select(u => u.FullName).FirstOrDefault(),
                r.ProcessedAt,
                r.Resolution,
                r.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class GetAdminReportHandler(IAppDbContext database)
{
    public async Task<ReportResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var report = await database.Reports
            .AsNoTracking()
            .Include(r => r.ReporterUser)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Report.NotFound);

        string? processedByName = null;
        if (report.ProcessedByUserId.HasValue)
        {
            processedByName = await database.Users
                .AsNoTracking()
                .Where(u => u.Id == report.ProcessedByUserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ReportResponse(
            report.Id,
            report.ReporterUserId,
            report.ReporterUser.FullName,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Details,
            report.Status,
            report.ProcessedByUserId,
            processedByName,
            report.ProcessedAt,
            report.Resolution,
            report.CreatedAt);
    }
}

public sealed class ResolveReportHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<ReportResponse> HandleAsync(
        Guid id,
        ResolveReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var report = await database.Reports
            .Include(r => r.ReporterUser)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Report.NotFound);

        if (report.Status != ReportStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Report.AlreadyProcessed);
        }

        report.Status = ReportStatus.Resolved;
        report.ProcessedByUserId = adminId;
        report.ProcessedAt = time.GetUtcNow();
        report.Resolution = request.Resolution?.Trim();

        await database.SaveChangesAsync(cancellationToken);

        var admin = await database.Users.AsNoTracking().FirstAsync(u => u.Id == adminId, cancellationToken);

        return new ReportResponse(
            report.Id,
            report.ReporterUserId,
            report.ReporterUser.FullName,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Details,
            report.Status,
            report.ProcessedByUserId,
            admin.FullName,
            report.ProcessedAt,
            report.Resolution,
            report.CreatedAt);
    }
}

public sealed class DismissReportHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<ReportResponse> HandleAsync(
        Guid id,
        DismissReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var report = await database.Reports
            .Include(r => r.ReporterUser)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Report.NotFound);

        if (report.Status != ReportStatus.Pending)
        {
            throw new BusinessRuleException(MessageKeys.Report.AlreadyProcessed);
        }

        report.Status = ReportStatus.Dismissed;
        report.ProcessedByUserId = adminId;
        report.ProcessedAt = time.GetUtcNow();
        report.Resolution = request.Resolution?.Trim();

        await database.SaveChangesAsync(cancellationToken);

        var admin = await database.Users.AsNoTracking().FirstAsync(u => u.Id == adminId, cancellationToken);

        return new ReportResponse(
            report.Id,
            report.ReporterUserId,
            report.ReporterUser.FullName,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Details,
            report.Status,
            report.ProcessedByUserId,
            admin.FullName,
            report.ProcessedAt,
            report.Resolution,
            report.CreatedAt);
    }
}
