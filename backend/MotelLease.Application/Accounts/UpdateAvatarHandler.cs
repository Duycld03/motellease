using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Application.Accounts;

/// <summary>
/// PUT /me/avatar. The previous image is deleted from storage after the new URL is saved —
/// the old project replaced the URL and left the file behind forever.
/// </summary>
public sealed class UpdateAvatarHandler(
    IAppDbContext database,
    IImageStorage imageStorage,
    ICurrentUser currentUser)
{
    /// <summary>Also used by the Api layer as the multipart request size limit.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public async Task<AvatarResponse> HandleAsync(
        UpdateAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Length > MaxBytes)
        {
            throw new BusinessRuleException(
                MessageKeys.Account.AvatarTooLarge, MaxBytes / (1024 * 1024));
        }

        // Content type is checked before anything is uploaded; the storage provider is not
        // the place to find out the file is a video.
        if (!AllowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                MessageKeys.Account.AvatarTypeNotSupported,
                string.Join(", ", AllowedContentTypes));
        }

        var userId = currentUser.RequireUserId();

        var user = await database.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        var previousPublicId = user.AvatarPublicId;

        var stored = await imageStorage.UploadAsync(
            new ImageUpload(request.Content, request.FileName, request.ContentType, request.Length),
            folder: "avatars",
            cancellationToken);

        user.AvatarUrl = stored.Url;
        user.AvatarPublicId = stored.PublicId;

        await database.SaveChangesAsync(cancellationToken);

        // Deleted only once the new avatar is committed: losing the old file after a failed
        // save would leave the account pointing at nothing.
        if (previousPublicId is not null)
        {
            await imageStorage.DeleteAsync(previousPublicId, cancellationToken);
        }

        return new AvatarResponse(stored.Url);
    }
}
