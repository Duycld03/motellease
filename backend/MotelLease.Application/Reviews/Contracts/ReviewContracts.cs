namespace MotelLease.Application.Reviews.Contracts;

public sealed record CreateReviewRequest(
    Guid BoardingHouseId,
    short Rating,
    string Content,
    IReadOnlyList<string>? ImageUrls = null);

public sealed record UpdateReviewRequest(
    short Rating,
    string Content);

public sealed record ReplyReviewRequest(
    string Content);

public sealed record ReviewResponse(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string? UserAvatarUrl,
    Guid BoardingHouseId,
    string BoardingHouseName,
    Guid? LeaseId,
    short? Rating,
    string Content,
    bool IsVerified,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<ReviewReplyResponse> Replies);

public sealed record ReviewReplyResponse(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string? UserAvatarUrl,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
