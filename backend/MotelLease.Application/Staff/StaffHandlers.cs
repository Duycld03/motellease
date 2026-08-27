using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Staff.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Staff;

public sealed class ListStaffHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<StaffSummaryResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();

        return await database.StaffProfiles
            .AsNoTracking()
            .Where(sp => sp.CreatedByUserId == ownerId && !sp.User.IsDeleted)
            .OrderByDescending(sp => sp.CreatedAt)
            .Select(sp => new StaffSummaryResponse(
                sp.UserId,
                sp.User.Username,
                sp.User.Email,
                sp.User.FullName,
                sp.User.PhoneNumber,
                sp.User.Gender,
                sp.User.IsLocked,
                sp.HireDate,
                database.StaffAssignments.Count(sa => sa.StaffUserId == sp.UserId && sa.UnassignedAt == null),
                sp.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public sealed class CreateStaffHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    IPasswordHasher hasher)
{
    public async Task<StaffDetailResponse> HandleAsync(
        CreateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim();

        if (await database.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Auth.EmailTaken, email);
        }

        if (await database.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Auth.UsernameTaken, username);
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Gender = request.Gender,
            Role = UserRole.Staff,
            EmailConfirmed = true,
            IsLocked = false
        };

        var profile = new StaffProfile
        {
            UserId = user.Id,
            User = user,
            HireDate = request.HireDate,
            CreatedByUserId = ownerId
        };

        database.Users.Add(user);
        database.StaffProfiles.Add(profile);
        await database.SaveChangesAsync(cancellationToken);

        return new StaffDetailResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.IsLocked,
            profile.HireDate,
            [],
            profile.CreatedAt);
    }
}

public sealed class GetStaffHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<StaffDetailResponse> HandleAsync(
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();

        var profile = await database.StaffProfiles
            .AsNoTracking()
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == staffUserId && sp.CreatedByUserId == ownerId && !sp.User.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotFound);

        var assignments = await database.StaffAssignments
            .AsNoTracking()
            .Include(sa => sa.BoardingHouse)
            .Where(sa => sa.StaffUserId == staffUserId && sa.UnassignedAt == null)
            .OrderByDescending(sa => sa.AssignedAt)
            .Select(sa => new StaffAssignmentResponse(
                sa.Id,
                sa.BoardingHouseId,
                sa.BoardingHouse.Name,
                sa.StaffUserId,
                profile.User.FullName,
                sa.AssignedAt))
            .ToListAsync(cancellationToken);

        return new StaffDetailResponse(
            profile.User.Id,
            profile.User.Username,
            profile.User.Email,
            profile.User.FullName,
            profile.User.PhoneNumber,
            profile.User.Gender,
            profile.User.IsLocked,
            profile.HireDate,
            assignments,
            profile.CreatedAt);
    }
}

public sealed class UpdateStaffHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<StaffDetailResponse> HandleAsync(
        Guid staffUserId,
        UpdateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();

        var profile = await database.StaffProfiles
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == staffUserId && sp.CreatedByUserId == ownerId && !sp.User.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotFound);

        profile.User.FullName = request.FullName.Trim();
        profile.User.PhoneNumber = request.PhoneNumber?.Trim();
        profile.User.Gender = request.Gender;
        profile.HireDate = request.HireDate;

        await database.SaveChangesAsync(cancellationToken);

        var assignments = await database.StaffAssignments
            .AsNoTracking()
            .Include(sa => sa.BoardingHouse)
            .Where(sa => sa.StaffUserId == staffUserId && sa.UnassignedAt == null)
            .OrderByDescending(sa => sa.AssignedAt)
            .Select(sa => new StaffAssignmentResponse(
                sa.Id,
                sa.BoardingHouseId,
                sa.BoardingHouse.Name,
                sa.StaffUserId,
                profile.User.FullName,
                sa.AssignedAt))
            .ToListAsync(cancellationToken);

        return new StaffDetailResponse(
            profile.User.Id,
            profile.User.Username,
            profile.User.Email,
            profile.User.FullName,
            profile.User.PhoneNumber,
            profile.User.Gender,
            profile.User.IsLocked,
            profile.HireDate,
            assignments,
            profile.CreatedAt);
    }
}

public sealed class LockStaffHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task HandleAsync(
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();

        var profile = await database.StaffProfiles
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == staffUserId && sp.CreatedByUserId == ownerId && !sp.User.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotFound);

        profile.User.IsLocked = true;
        profile.User.IsDeleted = true;

        // Unassign from all properties
        var activeAssignments = await database.StaffAssignments
            .Where(sa => sa.StaffUserId == staffUserId && sa.UnassignedAt == null)
            .ToListAsync(cancellationToken);

        var now = time.GetUtcNow();
        foreach (var assignment in activeAssignments)
        {
            assignment.UnassignedAt = now;
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListBoardingHouseStaffHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<IReadOnlyList<StaffAssignmentResponse>> HandleAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        return await database.StaffAssignments
            .AsNoTracking()
            .Include(sa => sa.StaffUser)
            .Where(sa => sa.BoardingHouseId == house.Id && sa.UnassignedAt == null)
            .OrderByDescending(sa => sa.AssignedAt)
            .Select(sa => new StaffAssignmentResponse(
                sa.Id,
                sa.BoardingHouseId,
                house.Name,
                sa.StaffUserId,
                sa.StaffUser.FullName,
                sa.AssignedAt))
            .ToListAsync(cancellationToken);
    }
}

public sealed class AssignStaffHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    BoardingHouseAccess access,
    TimeProvider time)
{
    public async Task<StaffAssignmentResponse> HandleAsync(
        Guid boardingHouseId,
        AssignStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();
        var house = await access.RequireOwnerAsync(boardingHouseId, cancellationToken);

        var staffProfile = await database.StaffProfiles
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == request.StaffUserId && sp.CreatedByUserId == ownerId && !sp.User.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotFound);

        var alreadyAssigned = await database.StaffAssignments
            .AnyAsync(sa => sa.BoardingHouseId == house.Id && sa.StaffUserId == request.StaffUserId && sa.UnassignedAt == null, cancellationToken);

        if (alreadyAssigned)
        {
            throw new ConflictException(MessageKeys.Staff.AlreadyAssigned);
        }

        var assignment = new StaffAssignment
        {
            BoardingHouseId = house.Id,
            StaffUserId = request.StaffUserId,
            AssignedByUserId = ownerId,
            AssignedAt = time.GetUtcNow()
        };

        database.StaffAssignments.Add(assignment);
        await database.SaveChangesAsync(cancellationToken);

        return new StaffAssignmentResponse(
            assignment.Id,
            house.Id,
            house.Name,
            staffProfile.UserId,
            staffProfile.User.FullName,
            assignment.AssignedAt);
    }
}

public sealed class UnassignStaffHandler(
    IAppDbContext database,
    BoardingHouseAccess access,
    TimeProvider time)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        Guid staffUserId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireOwnerAsync(boardingHouseId, cancellationToken);

        var assignment = await database.StaffAssignments
            .FirstOrDefaultAsync(sa => sa.BoardingHouseId == house.Id && sa.StaffUserId == staffUserId && sa.UnassignedAt == null, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Staff.NotAssigned);

        assignment.UnassignedAt = time.GetUtcNow();
        await database.SaveChangesAsync(cancellationToken);
    }
}
