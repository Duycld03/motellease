namespace MotelLease.Application.Common;

/// <summary>
/// What the API accepts as an image, in one place: avatars and listing photos go to the same
/// storage provider and have no reason to disagree on the limit. The message key stays with
/// the caller, because "avatar too large" and "listing photo too large" read differently.
/// </summary>
public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public const int MaxMegabytes = (int)(MaxBytes / (1024 * 1024));

    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public static string AllowedContentTypeList => string.Join(", ", AllowedContentTypes);

    public static bool IsAllowedContentType(string? contentType) =>
        contentType is not null
        && AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}
