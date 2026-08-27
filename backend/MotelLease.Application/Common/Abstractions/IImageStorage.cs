namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// Image hosting. The public id is kept alongside the URL so a replaced image can be
/// deleted instead of leaking storage, which the old project never did.
/// </summary>
public interface IImageStorage
{
    Task<StoredImage> UploadAsync(
        ImageUpload upload,
        string folder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

public sealed record ImageUpload(Stream Content, string FileName, string ContentType, long Length);

public sealed record StoredImage(string Url, string PublicId, int Width, int Height);
