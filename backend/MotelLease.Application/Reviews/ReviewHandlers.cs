using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Reviews.Contracts;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Reviews;

public sealed class CreateReviewHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<ReviewResponse> HandleAsync(
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var house = await database.BoardingHouses
            .FirstOrDefaultAsync(b => b.Id == request.BoardingHouseId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

        // Invariant §9.10: A top-level Review requires the user to have a Lease for that house
        var lease = await database.Leases
            .Include(l => l.Tenants)
            .Where(l => l.Room.BoardingHouseId == request.BoardingHouseId &&
                        l.Tenants.Any(t => t.UserId == userId))
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException(MessageKeys.Review.LeaseRequired);

        // One review per (UserId, LeaseId)
        var alreadyReviewed = await database.Reviews
            .AnyAsync(r => r.UserId == userId &&
                           r.LeaseId == lease.Id &&
                           !r.IsDeleted, cancellationToken);
        if (alreadyReviewed)
        {
            throw new BusinessRuleException(MessageKeys.Review.AlreadyReviewed);
        }

        var review = new Review
        {
            UserId = userId,
            BoardingHouseId = request.BoardingHouseId,
            LeaseId = lease.Id,
            Rating = request.Rating,
            Content = request.Content.Trim()
        };

        database.Reviews.Add(review);

        // Recalculate BoardingHouse Rating & ReviewCount
        var existingRatings = await database.Reviews
            .Where(r => r.BoardingHouseId == house.Id && r.ParentReviewId == null && !r.IsDeleted)
            .Select(r => (int)(r.Rating ?? 5))
            .ToListAsync(cancellationToken);

        existingRatings.Add(request.Rating);
        house.ReviewCount = existingRatings.Count;
        house.Rating = Math.Round((decimal)existingRatings.Average(), 1);

        if (request.ImageUrls is { Count: > 0 } urls)
        {
            for (var i = 0; i < urls.Count; i++)
            {
                database.Images.Add(new Image
                {
                    OwnerType = ImageOwnerType.Review,
                    OwnerId = review.Id,
                    Url = urls[i].Trim(),
                    PublicId = $"review-{review.Id}-{i}",
                    SortOrder = i
                });
            }
        }

        await database.SaveChangesAsync(cancellationToken);

        var user = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new ReviewResponse(
            review.Id,
            review.UserId,
            user.FullName,
            user.AvatarUrl,
            house.Id,
            house.Name,
            review.LeaseId,
            review.Rating,
            review.Content,
            review.LeaseId != null,
            review.CreatedAt,
            review.UpdatedAt,
            []);
    }
}

public sealed class UpdateReviewHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<ReviewResponse> HandleAsync(
        Guid reviewId,
        UpdateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var review = await database.Reviews
            .Include(r => r.BoardingHouse)
            .Include(r => r.User)
            .Include(r => r.Replies)
                .ThenInclude(rep => rep.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        if (review.UserId != userId)
        {
            throw new ForbiddenException(MessageKeys.Review.NotYours);
        }

        review.Rating = request.Rating;
        review.Content = request.Content.Trim();

        // Recalculate property rating
        var house = review.BoardingHouse;
        var existingRatings = await database.Reviews
            .Where(r => r.BoardingHouseId == house.Id && r.ParentReviewId == null && !r.IsDeleted && r.Id != review.Id)
            .Select(r => (int)(r.Rating ?? 5))
            .ToListAsync(cancellationToken);

        existingRatings.Add(request.Rating);
        house.ReviewCount = existingRatings.Count;
        house.Rating = Math.Round((decimal)existingRatings.Average(), 1);

        await database.SaveChangesAsync(cancellationToken);

        return MapReviewResponse(review);
    }

    internal static ReviewResponse MapReviewResponse(Review r) =>
        new(
            r.Id,
            r.UserId,
            r.User.FullName,
            r.User.AvatarUrl,
            r.BoardingHouseId,
            r.BoardingHouse?.Name ?? string.Empty,
            r.LeaseId,
            r.Rating,
            r.Content,
            r.LeaseId != null,
            r.CreatedAt,
            r.UpdatedAt,
            r.Replies
                .Where(rep => !rep.IsDeleted)
                .OrderBy(rep => rep.CreatedAt)
                .Select(rep => new ReviewReplyResponse(
                    rep.Id,
                    rep.UserId,
                    rep.User.FullName,
                    rep.User.AvatarUrl,
                    rep.Content,
                    rep.CreatedAt,
                    rep.UpdatedAt))
                .ToList());
}

public sealed class DeleteReviewHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var review = await database.Reviews
            .Include(r => r.BoardingHouse)
            .Include(r => r.Replies)
            .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        if (review.UserId != userId && role != UserRole.Admin)
        {
            throw new ForbiddenException(MessageKeys.Review.NotYours);
        }

        review.IsDeleted = true;
        foreach (var reply in review.Replies)
        {
            reply.IsDeleted = true;
        }

        // Recalculate property rating & review count
        var house = review.BoardingHouse;
        var remainingRatings = await database.Reviews
            .Where(r => r.BoardingHouseId == house.Id && r.ParentReviewId == null && !r.IsDeleted && r.Id != review.Id)
            .Select(r => (int)(r.Rating ?? 5))
            .ToListAsync(cancellationToken);

        house.ReviewCount = remainingRatings.Count;
        house.Rating = remainingRatings.Count > 0 ? Math.Round((decimal)remainingRatings.Average(), 1) : 0m;

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ReplyReviewHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    BoardingHouseAccess access)
{
    public async Task<ReviewReplyResponse> HandleAsync(
        Guid reviewId,
        ReplyReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var parent = await database.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        if (parent.ParentReviewId != null)
        {
            throw new BusinessRuleException(MessageKeys.Review.CannotReplyToReply);
        }

        await access.RequireStaffOrOwnerAsync(parent.BoardingHouseId, cancellationToken);

        var reply = new Review
        {
            UserId = userId,
            BoardingHouseId = parent.BoardingHouseId,
            ParentReviewId = parent.Id,
            Rating = null,
            Content = request.Content.Trim()
        };

        database.Reviews.Add(reply);
        await database.SaveChangesAsync(cancellationToken);

        var user = await database.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);

        return new ReviewReplyResponse(
            reply.Id,
            reply.UserId,
            user.FullName,
            user.AvatarUrl,
            reply.Content,
            reply.CreatedAt,
            reply.UpdatedAt);
    }
}

public sealed class UpdateReviewReplyHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    BoardingHouseAccess access)
{
    public async Task<ReviewReplyResponse> HandleAsync(
        Guid reviewId,
        Guid replyId,
        ReplyReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var reply = await database.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == replyId && r.ParentReviewId == reviewId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        await access.RequireStaffOrOwnerAsync(reply.BoardingHouseId, cancellationToken);

        reply.Content = request.Content.Trim();
        await database.SaveChangesAsync(cancellationToken);

        return new ReviewReplyResponse(
            reply.Id,
            reply.UserId,
            reply.User.FullName,
            reply.User.AvatarUrl,
            reply.Content,
            reply.CreatedAt,
            reply.UpdatedAt);
    }
}

public sealed class DeleteReviewReplyHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid reviewId,
        Guid replyId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var role = currentUser.Role;

        var reply = await database.Reviews
            .FirstOrDefaultAsync(r => r.Id == replyId && r.ParentReviewId == reviewId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Review.NotFound);

        if (reply.UserId != userId && role != UserRole.Admin)
        {
            await access.RequireStaffOrOwnerAsync(reply.BoardingHouseId, cancellationToken);
        }

        reply.IsDeleted = true;
        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListUserReviewsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<ReviewResponse>> HandleAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var userId = currentUser.RequireUserId();

        var query = database.Reviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.ParentReviewId == null && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponse(
                r.Id,
                r.UserId,
                r.User.FullName,
                r.User.AvatarUrl,
                r.BoardingHouseId,
                r.BoardingHouse.Name,
                r.LeaseId,
                r.Rating,
                r.Content,
                r.LeaseId != null,
                r.CreatedAt,
                r.UpdatedAt,
                r.Replies
                    .Where(rep => !rep.IsDeleted)
                    .OrderBy(rep => rep.CreatedAt)
                    .Select(rep => new ReviewReplyResponse(
                        rep.Id,
                        rep.UserId,
                        rep.User.FullName,
                        rep.User.AvatarUrl,
                        rep.Content,
                        rep.CreatedAt,
                        rep.UpdatedAt))
                    .ToList()));

        return await Paged.FromAsync(query, page, pageSize, cancellationToken);
    }
}

public sealed class ListPropertyReviewsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<PagedResponse<ReviewResponse>> HandleAsync(
        Guid? boardingHouseId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var userId = currentUser.RequireUserId();

        var accessibleHouseIds = await database.BoardingHouses
            .AsNoTracking()
            .Where(b => b.OwnerUserId == userId ||
                        database.StaffAssignments.Any(sa => sa.BoardingHouseId == b.Id && sa.StaffUserId == userId))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var query = database.Reviews
            .AsNoTracking()
            .Where(r => accessibleHouseIds.Contains(r.BoardingHouseId) &&
                        r.ParentReviewId == null &&
                        !r.IsDeleted);

        if (boardingHouseId.HasValue)
        {
            query = query.Where(r => r.BoardingHouseId == boardingHouseId.Value);
        }

        var projected = query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponse(
                r.Id,
                r.UserId,
                r.User.FullName,
                r.User.AvatarUrl,
                r.BoardingHouseId,
                r.BoardingHouse.Name,
                r.LeaseId,
                r.Rating,
                r.Content,
                r.LeaseId != null,
                r.CreatedAt,
                r.UpdatedAt,
                r.Replies
                    .Where(rep => !rep.IsDeleted)
                    .OrderBy(rep => rep.CreatedAt)
                    .Select(rep => new ReviewReplyResponse(
                        rep.Id,
                        rep.UserId,
                        rep.User.FullName,
                        rep.User.AvatarUrl,
                        rep.Content,
                        rep.CreatedAt,
                        rep.UpdatedAt))
                    .ToList()));

        return await Paged.FromAsync(projected, page, pageSize, cancellationToken);
    }
}
