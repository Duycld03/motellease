using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Tasks.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Tasks;

public sealed class ListTasksHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<TaskResponse>> HandleAsync(
        Guid? boardingHouseId,
        Guid? assignedTo,
        WorkTaskStatus? status,
        TaskPriority? priority,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var query = database.Tasks
            .AsNoTracking()
            .Include(t => t.BoardingHouse)
            .Include(t => t.AssignedToUser)
            .AsQueryable();

        if (role == UserRole.Owner)
        {
            query = query.Where(t => t.BoardingHouse.OwnerUserId == userId);
        }
        else if (role == UserRole.Staff)
        {
            var assignedHouseIds = database.StaffAssignments
                .Where(sa => sa.StaffUserId == userId && sa.UnassignedAt == null)
                .Select(sa => sa.BoardingHouseId);

            query = query.Where(t => t.AssignedToUserId == userId || assignedHouseIds.Contains(t.BoardingHouseId));
        }

        if (boardingHouseId.HasValue)
        {
            query = query.Where(t => t.BoardingHouseId == boardingHouseId.Value);
        }

        if (assignedTo.HasValue)
        {
            query = query.Where(t => t.AssignedToUserId == assignedTo.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        var projected = query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskResponse(
                t.Id,
                t.BoardingHouseId,
                t.BoardingHouse.Name,
                t.CreatedByUserId,
                t.AssignedToUserId,
                t.AssignedToUser.FullName,
                t.MaintenanceRequestId,
                t.Title,
                t.Details,
                t.Priority,
                t.Status,
                t.DueDate,
                t.CompletedAt,
                t.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class CreateTaskHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    BoardingHouseAccess access)
{
    public async Task<TaskResponse> HandleAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var creatorId = currentUser.RequireUserId();
        var house = await access.RequireStaffOrOwnerAsync(request.BoardingHouseId, cancellationToken);

        var assignedUser = await database.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.AssignedToUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotFound);

        var task = new WorkTask
        {
            BoardingHouseId = house.Id,
            CreatedByUserId = creatorId,
            AssignedToUserId = assignedUser.Id,
            Title = request.Title.Trim(),
            Details = request.Details?.Trim(),
            Priority = request.Priority,
            Status = WorkTaskStatus.InProgress,
            DueDate = request.DueDate
        };

        database.Tasks.Add(task);
        await database.SaveChangesAsync(cancellationToken);

        return new TaskResponse(
            task.Id,
            house.Id,
            house.Name,
            task.CreatedByUserId,
            assignedUser.Id,
            assignedUser.FullName,
            null,
            task.Title,
            task.Details,
            task.Priority,
            task.Status,
            task.DueDate,
            task.CompletedAt,
            task.CreatedAt);
    }
}

public sealed class GetTaskHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<TaskResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var task = await database.Tasks
            .AsNoTracking()
            .Include(t => t.BoardingHouse)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Task.NotFound);

        var isAssignedStaff = role == UserRole.Staff && (
            task.AssignedToUserId == userId ||
            await database.StaffAssignments.AnyAsync(sa => sa.BoardingHouseId == task.BoardingHouseId && sa.StaffUserId == userId && sa.UnassignedAt == null, cancellationToken));

        var hasAccess = role switch
        {
            UserRole.Admin => true,
            UserRole.Owner => task.BoardingHouse.OwnerUserId == userId,
            UserRole.Staff => isAssignedStaff,
            _ => false
        };

        if (!hasAccess)
        {
            throw new ForbiddenException(MessageKeys.Task.NotYours);
        }

        return new TaskResponse(
            task.Id,
            task.BoardingHouseId,
            task.BoardingHouse.Name,
            task.CreatedByUserId,
            task.AssignedToUserId,
            task.AssignedToUser.FullName,
            task.MaintenanceRequestId,
            task.Title,
            task.Details,
            task.Priority,
            task.Status,
            task.DueDate,
            task.CompletedAt,
            task.CreatedAt);
    }
}

public sealed class UpdateTaskHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<TaskResponse> HandleAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await database.Tasks
            .Include(t => t.BoardingHouse)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Task.NotFound);

        await access.RequireStaffOrOwnerAsync(task.BoardingHouseId, cancellationToken);

        var assignedUser = await database.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.AssignedToUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotFound);

        task.AssignedToUserId = assignedUser.Id;
        task.Title = request.Title.Trim();
        task.Details = request.Details?.Trim();
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;

        await database.SaveChangesAsync(cancellationToken);

        return new TaskResponse(
            task.Id,
            task.BoardingHouseId,
            task.BoardingHouse.Name,
            task.CreatedByUserId,
            assignedUser.Id,
            assignedUser.FullName,
            task.MaintenanceRequestId,
            task.Title,
            task.Details,
            task.Priority,
            task.Status,
            task.DueDate,
            task.CompletedAt,
            task.CreatedAt);
    }
}

public sealed class UpdateTaskStatusHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<TaskResponse> HandleAsync(
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var task = await database.Tasks
            .Include(t => t.BoardingHouse)
            .Include(t => t.AssignedToUser)
            .Include(t => t.MaintenanceRequest)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Task.NotFound);

        var isAssignedStaff = role == UserRole.Staff && (
            task.AssignedToUserId == userId ||
            await database.StaffAssignments.AnyAsync(sa => sa.BoardingHouseId == task.BoardingHouseId && sa.StaffUserId == userId && sa.UnassignedAt == null, cancellationToken));

        var hasAccess = role switch
        {
            UserRole.Admin => true,
            UserRole.Owner => task.BoardingHouse.OwnerUserId == userId,
            UserRole.Staff => isAssignedStaff,
            _ => false
        };

        if (!hasAccess)
        {
            throw new ForbiddenException(MessageKeys.Task.NotYours);
        }

        task.Status = request.Status;
        if (request.Status == WorkTaskStatus.Completed)
        {
            task.CompletedAt = time.GetUtcNow();
            if (task.MaintenanceRequest != null && task.MaintenanceRequest.Status != MaintenanceStatus.Resolved)
            {
                task.MaintenanceRequest.Status = MaintenanceStatus.Resolved;
            }
        }
        else
        {
            task.CompletedAt = null;
        }

        await database.SaveChangesAsync(cancellationToken);

        return new TaskResponse(
            task.Id,
            task.BoardingHouseId,
            task.BoardingHouse.Name,
            task.CreatedByUserId,
            task.AssignedToUserId,
            task.AssignedToUser.FullName,
            task.MaintenanceRequestId,
            task.Title,
            task.Details,
            task.Priority,
            task.Status,
            task.DueDate,
            task.CompletedAt,
            task.CreatedAt);
    }
}
