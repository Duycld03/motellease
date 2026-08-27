using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Infrastructure.Storage;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string? CloudName { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>User-secrets locally, GitHub Actions secrets in CI — never committed.</summary>
    public string? ApiSecret { get; set; }

    /// <summary>Prefix for every upload, so one account can host several environments.</summary>
    public string RootFolder { get; set; } = "motellease";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}

public sealed class CloudinaryImageStorage : IImageStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryImageStorage> _logger;

    public CloudinaryImageStorage(
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryImageStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        _cloudinary = new Cloudinary(
            new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret))
        {
            Api = { Secure = true }
        };
    }

    public async Task<StoredImage> UploadAsync(
        ImageUpload upload,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var parameters = new ImageUploadParams
        {
            File = new FileDescription(upload.FileName, upload.Content),
            Folder = $"{_options.RootFolder}/{folder}",
            // Cloudinary derives the type from the bytes, not from the client's claim.
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(parameters, cancellationToken);

        if (result.Error is not null)
        {
            // The caller sent bytes Cloudinary will not accept — a text file renamed to .png
            // passes the content-type check but fails here. That is the client's mistake, so it
            // must not surface as a 500.
            _logger.LogWarning(
                "Cloudinary rejected an upload: {Error}", result.Error.Message);
            throw new BusinessRuleException(MessageKeys.Image.Rejected);
        }

        return new StoredImage(
            result.SecureUrl.ToString(),
            result.PublicId,
            result.Width,
            result.Height);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        // Failure here leaves an orphaned file, which is not worth failing the request over —
        // the database already points at the new image.
        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
    }
}

/// <summary>
/// Registered when no Cloudinary credentials are present. Fails loudly on use instead of
/// letting an avatar upload appear to succeed and write a null URL.
/// </summary>
public sealed class UnconfiguredImageStorage : IImageStorage
{
    private const string Message =
        "Image storage is not configured. Set Cloudinary:CloudName, Cloudinary:ApiKey and " +
        "Cloudinary:ApiSecret through user-secrets or the environment.";

    public Task<StoredImage> UploadAsync(
        ImageUpload upload,
        string folder,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string publicId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);
}
