using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Admin;

// ==========================================
// Accounts
// ==========================================

public sealed class AdminListAccountsHandler(IAppDbContext database)
{
    public async Task<PagedResponse<AdminAccountSummaryResponse>> HandleAsync(
        UserRole? role,
        bool? isLocked,
        string? search,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = database.Users.AsNoTracking().AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (isLocked.HasValue)
        {
            query = query.Where(u => u.IsLocked == isLocked.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.Username.ToLower().Contains(term) ||
                u.FullName.ToLower().Contains(term) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var projected = query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminAccountSummaryResponse(
                u.Id,
                u.Email,
                u.Username,
                u.FullName,
                u.PhoneNumber,
                u.Gender,
                u.AvatarUrl,
                u.Role,
                u.EmailConfirmed,
                u.IsLocked,
                u.LockedReason,
                u.IsDeleted,
                u.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class AdminCreateAccountHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    IPasswordHasher passwords,
    AuditLogger auditLogger)
{
    public async Task<AdminAccountSummaryResponse> HandleAsync(
        AdminCreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();

        var emailTaken = await database.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken) throw new ConflictException(MessageKeys.Auth.EmailTaken);

        var usernameTaken = await database.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username, cancellationToken);
        if (usernameTaken) throw new ConflictException(MessageKeys.Auth.UsernameTaken);

        var user = new User
        {
            Email = email,
            Username = username,
            PasswordHash = passwords.Hash(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Gender = request.Gender,
            Role = request.Role,
            EmailConfirmed = true,
            IsLocked = false
        };

        if (user.Role == UserRole.Owner)
        {
            user.OwnerProfile = new OwnerProfile { User = user };
        }
        else if (user.Role == UserRole.Staff)
        {
            user.StaffProfile = new StaffProfile
            {
                User = user,
                CreatedByUserId = adminId,
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };
        }

        database.Users.Add(user);
        auditLogger.Log(adminId, "Account.Create", "User", user.Id, (object?)null, new { user.Email, user.Role });

        await database.SaveChangesAsync(cancellationToken);

        return new AdminAccountSummaryResponse(
            user.Id,
            user.Email,
            user.Username,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.AvatarUrl,
            user.Role,
            user.EmailConfirmed,
            user.IsLocked,
            user.LockedReason,
            user.IsDeleted,
            user.CreatedAt);
    }
}

public sealed class AdminGetAccountHandler(IAppDbContext database)
{
    public async Task<AdminAccountDetailResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await database.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        var housesCount = await database.BoardingHouses
            .IgnoreQueryFilters()
            .CountAsync(h => h.OwnerUserId == id, cancellationToken);

        var activeLeasesCount = await database.Leases
            .CountAsync(l => l.PrimaryTenantUserId == id && l.Status == LeaseStatus.Active, cancellationToken);

        return new AdminAccountDetailResponse(
            user.Id,
            user.Email,
            user.Username,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.AvatarUrl,
            user.Role,
            user.EmailConfirmed,
            user.IsLocked,
            user.LockedReason,
            user.IsDeleted,
            housesCount,
            activeLeasesCount,
            user.OwnerProfile?.AvailableBalance,
            user.CreatedAt);
    }
}

public sealed class AdminUpdateAccountHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task<AdminAccountSummaryResponse> HandleAsync(
        Guid id,
        AdminUpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var user = await database.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        var beforeState = new { user.FullName, user.PhoneNumber, user.Gender, user.Role };

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.Gender = request.Gender;

        if (user.Role != request.Role)
        {
            user.Role = request.Role;
            if (user.Role == UserRole.Owner && user.OwnerProfile == null)
            {
                user.OwnerProfile = new OwnerProfile { User = user };
            }
        }

        var afterState = new { user.FullName, user.PhoneNumber, user.Gender, user.Role };
        auditLogger.Log(adminId, "Account.Update", "User", user.Id, beforeState, afterState);

        await database.SaveChangesAsync(cancellationToken);

        return new AdminAccountSummaryResponse(
            user.Id,
            user.Email,
            user.Username,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.AvatarUrl,
            user.Role,
            user.EmailConfirmed,
            user.IsLocked,
            user.LockedReason,
            user.IsDeleted,
            user.CreatedAt);
    }
}

public sealed class AdminDeleteAccountHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();
        if (id == adminId)
        {
            throw new BusinessRuleException(MessageKeys.Admin.CannotDeleteSelf);
        }

        var user = await database.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.IsDeleted = true;

        var tokens = await database.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync(cancellationToken);
        var now = time.GetUtcNow();
        foreach (var t in tokens) t.RevokedAt = now;

        auditLogger.Log(adminId, "Account.Delete", "User", id, (object?)null, new { IsDeleted = true });

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AdminRestoreAccountHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var user = await database.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.IsDeleted = false;
        auditLogger.Log(adminId, "Account.Restore", "User", id, (object?)null, new { IsDeleted = false });

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AdminLockAccountHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        AdminLockAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();
        if (id == adminId)
        {
            throw new BusinessRuleException(MessageKeys.Admin.CannotLockSelf);
        }

        var user = await database.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.IsLocked = true;
        user.LockedReason = request.Reason?.Trim();

        var tokens = await database.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync(cancellationToken);
        var now = time.GetUtcNow();
        foreach (var t in tokens) t.RevokedAt = now;

        auditLogger.Log(adminId, "Account.Lock", "User", id, (object?)null, new { user.IsLocked, user.LockedReason });

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AdminUnlockAccountHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var user = await database.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.IsLocked = false;
        user.LockedReason = null;

        auditLogger.Log(adminId, "Account.Unlock", "User", id, (object?)null, new { user.IsLocked });

        await database.SaveChangesAsync(cancellationToken);
    }
}

// ==========================================
// Boarding Houses
// ==========================================

public sealed class AdminListBoardingHousesHandler(IAppDbContext database)
{
    public async Task<PagedResponse<AdminBoardingHouseResponse>> HandleAsync(
        ListingStatus? listingStatus,
        string? search,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = database.BoardingHouses
            .AsNoTracking()
            .Include(h => h.OwnerUser)
            .Include(h => h.Rooms)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (listingStatus.HasValue)
        {
            query = query.Where(h => h.ListingStatus == listingStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(h =>
                h.Name.ToLower().Contains(term) ||
                h.AddressLine.ToLower().Contains(term) ||
                h.OwnerUser.FullName.ToLower().Contains(term) ||
                h.OwnerUser.Email.ToLower().Contains(term));
        }

        var projected = query
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new AdminBoardingHouseResponse(
                h.Id,
                h.Name,
                h.AddressLine,
                h.Province,
                h.District,
                h.Ward,
                h.OwnerUserId,
                h.OwnerUser.FullName,
                h.OwnerUser.Email,
                h.ListingStatus,
                h.RejectionReason,
                h.IsDeleted,
                h.Rooms.Count(r => !r.IsDeleted),
                h.Rating,
                h.ReviewCount,
                h.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class AdminApproveBoardingHouseHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task<AdminBoardingHouseResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var house = await database.BoardingHouses
            .Include(h => h.OwnerUser)
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        house.ListingStatus = ListingStatus.Published;
        house.RejectionReason = null;

        auditLogger.Log(adminId, "BoardingHouse.Approve", "BoardingHouse", id, (object?)null, new { house.ListingStatus });

        await database.SaveChangesAsync(cancellationToken);

        return new AdminBoardingHouseResponse(
            house.Id,
            house.Name,
            house.AddressLine,
            house.Province,
            house.District,
            house.Ward,
            house.OwnerUserId,
            house.OwnerUser.FullName,
            house.OwnerUser.Email,
            house.ListingStatus,
            house.RejectionReason,
            house.IsDeleted,
            house.Rooms.Count(r => !r.IsDeleted),
            house.Rating,
            house.ReviewCount,
            house.CreatedAt);
    }
}

public sealed class AdminRejectBoardingHouseHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task<AdminBoardingHouseResponse> HandleAsync(
        Guid id,
        AdminRejectListingRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var house = await database.BoardingHouses
            .Include(h => h.OwnerUser)
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        house.ListingStatus = ListingStatus.Rejected;
        house.RejectionReason = request.Reason?.Trim();

        auditLogger.Log(adminId, "BoardingHouse.Reject", "BoardingHouse", id, (object?)null, new { house.ListingStatus, house.RejectionReason });

        await database.SaveChangesAsync(cancellationToken);

        return new AdminBoardingHouseResponse(
            house.Id,
            house.Name,
            house.AddressLine,
            house.Province,
            house.District,
            house.Ward,
            house.OwnerUserId,
            house.OwnerUser.FullName,
            house.OwnerUser.Email,
            house.ListingStatus,
            house.RejectionReason,
            house.IsDeleted,
            house.Rooms.Count(r => !r.IsDeleted),
            house.Rating,
            house.ReviewCount,
            house.CreatedAt);
    }
}

public sealed class AdminDeleteBoardingHouseHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var house = await database.BoardingHouses
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        house.IsDeleted = true;
        auditLogger.Log(adminId, "BoardingHouse.Delete", "BoardingHouse", id, (object?)null, new { IsDeleted = true });

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AdminRestoreBoardingHouseHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var house = await database.BoardingHouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        house.IsDeleted = false;
        auditLogger.Log(adminId, "BoardingHouse.Restore", "BoardingHouse", id, (object?)null, new { IsDeleted = false });

        await database.SaveChangesAsync(cancellationToken);
    }
}

// ==========================================
// Facilities
// ==========================================

public sealed class AdminListFacilitiesHandler(IAppDbContext database)
{
    public async Task<List<FacilityDetailResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await database.Facilities
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FacilityDetailResponse(
                f.Id,
                f.Name,
                f.CodeName,
                f.IconKey,
                f.Description,
                f.RoomTypes.Count(rt => !rt.IsDeleted),
                f.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public sealed class AdminCreateFacilityHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task<FacilityDetailResponse> HandleAsync(
        CreateFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();
        var name = request.Name.Trim();
        var codeName = (request.CodeName ?? name.ToLower().Replace(" ", "_")).Trim();

        var exists = await database.Facilities.AnyAsync(f => f.Name.ToLower() == name.ToLower() || f.CodeName.ToLower() == codeName.ToLower(), cancellationToken);
        if (exists) throw new ConflictException(MessageKeys.Facility.NameTaken);

        var facility = new Facility
        {
            Name = name,
            CodeName = codeName,
            IconKey = request.IconKey?.Trim(),
            Description = request.Description?.Trim()
        };

        database.Facilities.Add(facility);
        auditLogger.Log(adminId, "Facility.Create", "Facility", facility.Id, (object?)null, new { facility.Name, facility.CodeName });

        await database.SaveChangesAsync(cancellationToken);

        return new FacilityDetailResponse(facility.Id, facility.Name, facility.CodeName, facility.IconKey, facility.Description, 0, facility.CreatedAt);
    }
}

public sealed class AdminGetFacilityHandler(IAppDbContext database)
{
    public async Task<FacilityDetailResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var facility = await database.Facilities
            .AsNoTracking()
            .Include(f => f.RoomTypes)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Facility.NotFound);

        var count = facility.RoomTypes.Count(rt => !rt.IsDeleted);

        return new FacilityDetailResponse(facility.Id, facility.Name, facility.CodeName, facility.IconKey, facility.Description, count, facility.CreatedAt);
    }
}

public sealed class AdminUpdateFacilityHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task<FacilityDetailResponse> HandleAsync(
        Guid id,
        UpdateFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var facility = await database.Facilities
            .Include(f => f.RoomTypes)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Facility.NotFound);

        var name = request.Name.Trim();
        var codeName = (request.CodeName ?? facility.CodeName).Trim();

        var duplicate = await database.Facilities.AnyAsync(f => f.Id != id && (f.Name.ToLower() == name.ToLower() || f.CodeName.ToLower() == codeName.ToLower()), cancellationToken);
        if (duplicate) throw new ConflictException(MessageKeys.Facility.NameTaken);

        var beforeState = new { facility.Name, facility.CodeName, facility.IconKey, facility.Description };

        facility.Name = name;
        facility.CodeName = codeName;
        facility.IconKey = request.IconKey?.Trim();
        facility.Description = request.Description?.Trim();

        var afterState = new { facility.Name, facility.CodeName, facility.IconKey, facility.Description };
        auditLogger.Log(adminId, "Facility.Update", "Facility", id, beforeState, afterState);

        await database.SaveChangesAsync(cancellationToken);

        var count = facility.RoomTypes.Count(rt => !rt.IsDeleted);

        return new FacilityDetailResponse(facility.Id, facility.Name, facility.CodeName, facility.IconKey, facility.Description, count, facility.CreatedAt);
    }
}

public sealed class AdminDeleteFacilityHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var facility = await database.Facilities
            .Include(f => f.RoomTypes)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Facility.NotFound);

        var inUse = facility.RoomTypes.Any(rt => !rt.IsDeleted);
        if (inUse)
        {
            throw new ConflictException(MessageKeys.Facility.InUse);
        }

        database.Facilities.Remove(facility);
        auditLogger.Log(adminId, "Facility.Delete", "Facility", id, (object?)null, new { facility.Name });

        await database.SaveChangesAsync(cancellationToken);
    }
}

// ==========================================
// Reviews
// ==========================================

public sealed class AdminListReviewsHandler(IAppDbContext database)
{
    public async Task<PagedResponse<AdminReviewResponse>> HandleAsync(
        Guid? boardingHouseId,
        string? search,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = database.Reviews
            .AsNoTracking()
            .Include(r => r.BoardingHouse)
            .Include(r => r.User)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (boardingHouseId.HasValue)
        {
            query = query.Where(r => r.BoardingHouseId == boardingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.Content.ToLower().Contains(term) ||
                r.User.FullName.ToLower().Contains(term) ||
                r.BoardingHouse.Name.ToLower().Contains(term));
        }

        var projected = query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminReviewResponse(
                r.Id,
                r.BoardingHouseId,
                r.BoardingHouse.Name,
                r.UserId,
                r.User.FullName,
                r.Rating,
                r.Content,
                r.IsDeleted,
                r.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class AdminDeleteReviewHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var review = await database.Reviews
            .Include(r => r.BoardingHouse)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        review.IsDeleted = true;

        // Recalculate house ratings
        var validReviews = await database.Reviews
            .IgnoreQueryFilters()
            .Where(r => r.BoardingHouseId == review.BoardingHouseId && r.Id != id && r.ParentReviewId == null && !r.IsDeleted && r.Rating.HasValue)
            .ToListAsync(cancellationToken);

        review.BoardingHouse.ReviewCount = validReviews.Count;
        review.BoardingHouse.Rating = validReviews.Count > 0 ? Math.Round((decimal)validReviews.Average(r => (double)r.Rating!.Value), 1) : 0;

        auditLogger.Log(adminId, "Review.Delete", "Review", id, (object?)null, new { IsDeleted = true });

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AdminRestoreReviewHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    AuditLogger auditLogger)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var adminId = currentUser.RequireUserId();

        var review = await database.Reviews
            .IgnoreQueryFilters()
            .Include(r => r.BoardingHouse)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        review.IsDeleted = false;

        // Recalculate house ratings
        var validReviews = await database.Reviews
            .IgnoreQueryFilters()
            .Where(r => r.BoardingHouseId == review.BoardingHouseId && (r.Id == id || !r.IsDeleted) && r.ParentReviewId == null && r.Rating.HasValue)
            .ToListAsync(cancellationToken);

        review.BoardingHouse.ReviewCount = validReviews.Count;
        review.BoardingHouse.Rating = validReviews.Count > 0 ? Math.Round((decimal)validReviews.Average(r => (double)r.Rating!.Value), 1) : 0;

        auditLogger.Log(adminId, "Review.Restore", "Review", id, (object?)null, new { IsDeleted = false });

        await database.SaveChangesAsync(cancellationToken);
    }
}

// ==========================================
// Audit Logs & Stats
// ==========================================

public sealed class AdminListAuditLogsHandler(IAppDbContext database)
{
    public async Task<PagedResponse<AuditLogResponse>> HandleAsync(
        Guid? actorUserId,
        string? entityType,
        Guid? entityId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = database.AuditLogs.AsNoTracking().AsQueryable();

        if (actorUserId.HasValue) query = query.Where(l => l.ActorUserId == actorUserId.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(l => l.EntityType == entityType);
        if (entityId.HasValue) query = query.Where(l => l.EntityId == entityId.Value);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value);

        var projected = query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new AuditLogResponse(
                l.Id,
                l.ActorUserId,
                database.Users.Where(u => u.Id == l.ActorUserId).Select(u => u.FullName).FirstOrDefault(),
                l.Action,
                l.EntityType,
                l.EntityId,
                l.BeforeJson,
                l.AfterJson,
                l.IpAddress,
                l.CreatedAt));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}

public sealed class AdminGetStatsSummaryHandler(IAppDbContext database)
{
    public async Task<AdminPlatformStatsResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var users = await database.Users
            .AsNoTracking()
            .Select(u => u.Role)
            .ToListAsync(cancellationToken);

        var usersByRole = users
            .GroupBy(u => u.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var houses = await database.BoardingHouses
            .AsNoTracking()
            .Select(h => h.ListingStatus)
            .ToListAsync(cancellationToken);

        var housesByStatus = houses
            .GroupBy(h => h.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var rooms = await database.Rooms
            .AsNoTracking()
            .Select(r => r.Status)
            .ToListAsync(cancellationToken);

        var roomsByStatus = rooms
            .GroupBy(r => r.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var activeLeases = await database.Leases
            .AsNoTracking()
            .CountAsync(l => l.Status == LeaseStatus.Active, cancellationToken);

        var payments = await database.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .Select(p => p.Amount)
            .ToListAsync(cancellationToken);

        var pendingReports = await database.Reports
            .AsNoTracking()
            .CountAsync(r => r.Status == ReportStatus.Pending, cancellationToken);

        var pendingWithdrawals = await database.WithdrawRequests
            .AsNoTracking()
            .CountAsync(w => w.Status == RequestStatus.Pending, cancellationToken);

        var pendingListingReviews = houses.Count(h => h == ListingStatus.PendingReview);

        return new AdminPlatformStatsResponse(
            users.Count,
            usersByRole,
            houses.Count,
            housesByStatus,
            rooms.Count,
            roomsByStatus,
            activeLeases,
            payments.Count,
            payments.Sum(),
            pendingReports,
            pendingWithdrawals,
            pendingListingReviews);
    }
}
