using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Images.Contracts;

namespace MotelLease.Application.Images;

public sealed class UploadImageHandler(IImageStorage imageStorage)
{
    public async Task<UploadImageResponse> HandleAsync(
        UploadFileRequest request,
        string folder = "uploads",
        CancellationToken cancellationToken = default)
    {
        if (request.Length > ImageUploadRules.MaxBytes)
        {
            throw new BusinessRuleException(
                MessageKeys.Image.TooLarge, ImageUploadRules.MaxMegabytes);
        }

        if (!ImageUploadRules.IsAllowedContentType(request.ContentType))
        {
            throw new BusinessRuleException(
                MessageKeys.Image.TypeNotSupported, ImageUploadRules.AllowedContentTypeList);
        }

        var stored = await imageStorage.UploadAsync(
            new ImageUpload(request.Content, request.FileName, request.ContentType, request.Length),
            folder: folder,
            cancellationToken);

        return new UploadImageResponse(stored.Url, stored.PublicId, stored.Width, stored.Height);
    }
}

public sealed class DeleteImageHandler(
    IAppDbContext database,
    IImageStorage imageStorage)
{
    public async Task HandleAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;

        await imageStorage.DeleteAsync(publicId, cancellationToken);

        var records = await database.Images
            .Where(i => i.PublicId == publicId)
            .ToListAsync(cancellationToken);

        if (records.Count > 0)
        {
            database.Images.RemoveRange(records);
            await database.SaveChangesAsync(cancellationToken);
        }
    }
}
