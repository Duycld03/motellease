using Microsoft.EntityFrameworkCore;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Maintenance.Contracts;
using MotelLease.Application.Notifications;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Maintenance;

public sealed class ListMaintenanceRequestsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<MaintenanceRequestResponse>> HandleAsync(
        Guid? boardingHouseId,
        MaintenanceStatus? status,
        MaintenanceCategory? category,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var query = database.MaintenanceRequests
            .AsNoTracking()
            .Include(m => m.Lease)
            .Include(m => m.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(m => m.ReportedByUser)
            .Include(m => m.Task)
            .AsQueryable();

        if (role == UserRole.Tenant)
        {
            query = query.Where(m => m.ReportedByUserId == userId);
        }
        else if (role == UserRole.Owner)
        {
            query = query.Where(m => m.Room.BoardingHouse.OwnerUserId == userId);
        }
        else if (role == UserRole.Staff)
        {
            var assignedHouseIds = database.StaffAssignments
                .Where(sa => sa.StaffUserId == userId && sa.UnassignedAt == null)
                .Select(sa => sa.BoardingHouseId);

            query = query.Where(m => assignedHouseIds.Contains(m.Room.BoardingHouseId));
        }

        if (boardingHouseId.HasValue)
        {
            query = query.Where(m => m.Room.BoardingHouseId == boardingHouseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(m => m.Category == category.Value);
        }

        var projected = query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MaintenanceRequestResponse(
                m.Id,
                m.LeaseId,
                m.RoomId,
                m.Room.RoomNumber,
                m.Room.BoardingHouseId,
                m.Room.BoardingHouse.Name,
                m.ReportedByUserId,
                m.ReportedByUser.FullName,
                m.Category,
                m.Description,
                m.Status,
                m.Task != null ? m.Task.Id : (Guid?)null,
                database.Images
                    .Where(i => i.OwnerType == ImageOwnerType.MaintenanceRequest && i.OwnerId == m.Id)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
                    .ToList(),
                m.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class CreateMaintenanceRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    NotificationDispatcher notifications)
{
    public async Task<MaintenanceRequestResponse> HandleAsync(
        CreateMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var lease = await database.Leases
            .Include(l => l.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(l => l.Tenants)
            .FirstOrDefaultAsync(l => l.Id == request.LeaseId &&
                                      l.Tenants.Any(t => t.UserId == userId), cancellationToken)
            ?? throw new BusinessRuleException(MessageKeys.Maintenance.NoActiveLease);

        var maintenance = new MaintenanceRequest
        {
            LeaseId = lease.Id,
            RoomId = lease.RoomId,
            ReportedByUserId = userId,
            Category = request.Category,
            Description = request.Description.Trim(),
            Status = MaintenanceStatus.Open
        };

        database.MaintenanceRequests.Add(maintenance);

        var imageResponses = new List<ImageResponse>();
        if (request.ImageUrls is { Count: > 0 })
        {
            var order = 0;
            foreach (var url in request.ImageUrls)
            {
                var img = new Image
                {
                    OwnerType = ImageOwnerType.MaintenanceRequest,
                    OwnerId = maintenance.Id,
                    Url = url,
                    PublicId = $"maintenance/{maintenance.Id}/{order}",
                    SortOrder = order,
                    IsPrimary = order == 0
                };
                database.Images.Add(img);
                imageResponses.Add(new ImageResponse(img.Id, img.Url, img.IsPrimary, img.SortOrder));
                order++;
            }
        }

        // Notify assigned staff or owner
        var activeStaff = await database.StaffAssignments
            .Where(sa => sa.BoardingHouseId == lease.Room.BoardingHouseId && sa.UnassignedAt == null)
            .Select(sa => sa.StaffUserId)
            .ToListAsync(cancellationToken);

        var recipients = activeStaff.Count > 0
            ? activeStaff
            : new List<Guid> { lease.Room.BoardingHouse.OwnerUserId };

        foreach (var recipientId in recipients)
        {
            notifications.Queue(
                recipientId,
                NotificationType.MaintenanceReported,
                new
                {
                    boardingHouseName = lease.Room.BoardingHouse.Name,
                    roomNumber = lease.Room.RoomNumber,
                    category = request.Category.ToString()
                },
                $"/maintenance-requests/{maintenance.Id}");
        }

        await database.SaveChangesAsync(cancellationToken);
        await notifications.DeliverAsync(cancellationToken);

        var user = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new MaintenanceRequestResponse(
            maintenance.Id,
            maintenance.LeaseId,
            maintenance.RoomId,
            lease.Room.RoomNumber,
            lease.Room.BoardingHouseId,
            lease.Room.BoardingHouse.Name,
            user.Id,
            user.FullName,
            maintenance.Category,
            maintenance.Description,
            maintenance.Status,
            null,
            imageResponses,
            maintenance.CreatedAt);
    }
}

public sealed class GetMaintenanceRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<MaintenanceRequestResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var maintenance = await database.MaintenanceRequests
            .AsNoTracking()
            .Include(m => m.Lease)
            .Include(m => m.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(m => m.ReportedByUser)
            .Include(m => m.Task)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Maintenance.NotFound);

        var isAssignedStaff = role == UserRole.Staff && await database.StaffAssignments
            .AnyAsync(sa => sa.BoardingHouseId == maintenance.Room.BoardingHouseId && sa.StaffUserId == userId && sa.UnassignedAt == null, cancellationToken);

        var hasAccess = role switch
        {
            UserRole.Admin => true,
            UserRole.Tenant => maintenance.ReportedByUserId == userId,
            UserRole.Owner => maintenance.Room.BoardingHouse.OwnerUserId == userId,
            UserRole.Staff => isAssignedStaff,
            _ => false
        };

        if (!hasAccess)
        {
            throw new ForbiddenException(MessageKeys.Maintenance.NotYours);
        }

        var images = await database.Images
            .AsNoTracking()
            .Where(i => i.OwnerType == ImageOwnerType.MaintenanceRequest && i.OwnerId == maintenance.Id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

        return new MaintenanceRequestResponse(
            maintenance.Id,
            maintenance.LeaseId,
            maintenance.RoomId,
            maintenance.Room.RoomNumber,
            maintenance.Room.BoardingHouseId,
            maintenance.Room.BoardingHouse.Name,
            maintenance.ReportedByUserId,
            maintenance.ReportedByUser.FullName,
            maintenance.Category,
            maintenance.Description,
            maintenance.Status,
            maintenance.Task?.Id,
            images,
            maintenance.CreatedAt);
    }
}

public sealed class AcceptMaintenanceRequestHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    BoardingHouseAccess access)
{
    public async Task<MaintenanceRequestResponse> HandleAsync(
        Guid id,
        AcceptMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var maintenance = await database.MaintenanceRequests
            .Include(m => m.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(m => m.ReportedByUser)
            .Include(m => m.Task)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Maintenance.NotFound);

        await access.RequireStaffOrOwnerAsync(maintenance.Room.BoardingHouseId, cancellationToken);

        if (maintenance.Status != MaintenanceStatus.Open)
        {
            throw new BusinessRuleException(MessageKeys.Maintenance.AlreadyProcessed);
        }

        maintenance.Status = MaintenanceStatus.InProgress;

        Guid? taskId = null;
        if (request.AssignToStaffUserId.HasValue)
        {
            var task = new WorkTask
            {
                BoardingHouseId = maintenance.Room.BoardingHouseId,
                CreatedByUserId = userId,
                AssignedToUserId = request.AssignToStaffUserId.Value,
                MaintenanceRequestId = maintenance.Id,
                Title = request.TaskTitle?.Trim() ?? $"Sửa chữa: {maintenance.Category}",
                Details = maintenance.Description,
                Priority = TaskPriority.High,
                Status = WorkTaskStatus.InProgress,
                DueDate = request.DueDate
            };
            database.Tasks.Add(task);
            maintenance.Task = task;
            taskId = task.Id;
        }

        await database.SaveChangesAsync(cancellationToken);

        var images = await database.Images
            .AsNoTracking()
            .Where(i => i.OwnerType == ImageOwnerType.MaintenanceRequest && i.OwnerId == maintenance.Id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

        return new MaintenanceRequestResponse(
            maintenance.Id,
            maintenance.LeaseId,
            maintenance.RoomId,
            maintenance.Room.RoomNumber,
            maintenance.Room.BoardingHouseId,
            maintenance.Room.BoardingHouse.Name,
            maintenance.ReportedByUserId,
            maintenance.ReportedByUser.FullName,
            maintenance.Category,
            maintenance.Description,
            maintenance.Status,
            taskId ?? maintenance.Task?.Id,
            images,
            maintenance.CreatedAt);
    }
}

public sealed class ResolveMaintenanceRequestHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    TimeProvider time)
{
    public async Task<MaintenanceRequestResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var maintenance = await database.MaintenanceRequests
            .Include(m => m.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(m => m.ReportedByUser)
            .Include(m => m.Task)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Maintenance.NotFound);

        await access.RequireStaffOrOwnerAsync(maintenance.Room.BoardingHouseId, cancellationToken);

        if (maintenance.Status == MaintenanceStatus.Resolved || maintenance.Status == MaintenanceStatus.Rejected)
        {
            throw new BusinessRuleException(MessageKeys.Maintenance.AlreadyProcessed);
        }

        maintenance.Status = MaintenanceStatus.Resolved;

        if (maintenance.Task != null && maintenance.Task.Status != WorkTaskStatus.Completed)
        {
            maintenance.Task.Status = WorkTaskStatus.Completed;
            maintenance.Task.CompletedAt = time.GetUtcNow();
        }

        await database.SaveChangesAsync(cancellationToken);

        var images = await database.Images
            .AsNoTracking()
            .Where(i => i.OwnerType == ImageOwnerType.MaintenanceRequest && i.OwnerId == maintenance.Id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

        return new MaintenanceRequestResponse(
            maintenance.Id,
            maintenance.LeaseId,
            maintenance.RoomId,
            maintenance.Room.RoomNumber,
            maintenance.Room.BoardingHouseId,
            maintenance.Room.BoardingHouse.Name,
            maintenance.ReportedByUserId,
            maintenance.ReportedByUser.FullName,
            maintenance.Category,
            maintenance.Description,
            maintenance.Status,
            maintenance.Task?.Id,
            images,
            maintenance.CreatedAt);
    }
}

public sealed class RejectMaintenanceRequestHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<MaintenanceRequestResponse> HandleAsync(
        Guid id,
        RejectMaintenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var maintenance = await database.MaintenanceRequests
            .Include(m => m.Room)
                .ThenInclude(r => r.BoardingHouse)
            .Include(m => m.ReportedByUser)
            .Include(m => m.Task)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Maintenance.NotFound);

        await access.RequireStaffOrOwnerAsync(maintenance.Room.BoardingHouseId, cancellationToken);

        if (maintenance.Status == MaintenanceStatus.Resolved || maintenance.Status == MaintenanceStatus.Rejected)
        {
            throw new BusinessRuleException(MessageKeys.Maintenance.AlreadyProcessed);
        }

        maintenance.Status = MaintenanceStatus.Rejected;

        if (maintenance.Task != null && maintenance.Task.Status != WorkTaskStatus.Cancelled)
        {
            maintenance.Task.Status = WorkTaskStatus.Cancelled;
        }

        await database.SaveChangesAsync(cancellationToken);

        var images = await database.Images
            .AsNoTracking()
            .Where(i => i.OwnerType == ImageOwnerType.MaintenanceRequest && i.OwnerId == maintenance.Id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageResponse(i.Id, i.Url, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

        return new MaintenanceRequestResponse(
            maintenance.Id,
            maintenance.LeaseId,
            maintenance.RoomId,
            maintenance.Room.RoomNumber,
            maintenance.Room.BoardingHouseId,
            maintenance.Room.BoardingHouse.Name,
            maintenance.ReportedByUserId,
            maintenance.ReportedByUser.FullName,
            maintenance.Category,
            maintenance.Description,
            maintenance.Status,
            maintenance.Task?.Id,
            images,
            maintenance.CreatedAt);
    }
}
