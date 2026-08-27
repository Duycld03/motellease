using Microsoft.EntityFrameworkCore;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.BoardingHouses;

/// <summary>
/// POST /my/boarding-houses/{id}/images. The remote file and the row are created in the same
/// flow, so storage never ends up holding a picture nothing points at.
/// </summary>
public sealed class AddBoardingHouseImageHandler(
    IAppDbContext database,
    IImageStorage imageStorage,
    BoardingHouseAccess access)
{
    /// <summary>
    /// A gallery, not an archive. The cap exists because nothing else limits how many files one
    /// listing can push into storage.
    /// </summary>
    public const int MaxImages = 20;

    public async Task<ImageResponse> HandleAsync(
        Guid boardingHouseId,
        AddImageRequest request,
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

        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        var existing = await database.Images
            .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == house.Id)
            .Select(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        if (existing.Count >= MaxImages)
        {
            throw new BusinessRuleException(MessageKeys.BoardingHouse.TooManyImages, MaxImages);
        }

        var stored = await imageStorage.UploadAsync(
            new ImageUpload(request.Content, request.FileName, request.ContentType, request.Length),
            folder: "boarding-houses",
            cancellationToken);

        var image = new Image
        {
            OwnerType = ImageOwnerType.BoardingHouse,
            OwnerId = house.Id,
            Url = stored.Url,
            PublicId = stored.PublicId,
            // The first upload is the cover by default, so a listing is never left without one.
            IsPrimary = existing.Count == 0,
            SortOrder = existing.Count == 0 ? 0 : existing.Max() + 1
        };

        database.Images.Add(image);

        await database.SaveChangesAsync(cancellationToken);

        return new ImageResponse(image.Id, image.Url, image.IsPrimary, image.SortOrder);
    }
}

/// <summary>
/// DELETE /my/boarding-houses/{id}/images/{imageId}. Removing the cover promotes the next
/// picture, because a listing that still has images must still have one marked primary.
/// </summary>
public sealed class DeleteBoardingHouseImageHandler(
    IAppDbContext database,
    IImageStorage imageStorage,
    BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        var images = await database.Images
            .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == house.Id)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        var image = images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new NotFoundException(MessageKeys.Image.NotFound);

        database.Images.Remove(image);

        if (image.IsPrimary)
        {
            var next = images.FirstOrDefault(i => i.Id != image.Id);

            if (next is not null)
            {
                next.IsPrimary = true;
            }
        }

        await database.SaveChangesAsync(cancellationToken);

        // After the row is gone: a failed delete here leaves an unreferenced file, while the
        // reverse order would leave the listing pointing at a picture that no longer exists.
        await imageStorage.DeleteAsync(image.PublicId, cancellationToken);
    }
}

/// <summary>PUT /my/boarding-houses/{id}/images/{imageId}/primary.</summary>
public sealed class SetPrimaryBoardingHouseImageHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid boardingHouseId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireStaffOrOwnerAsync(boardingHouseId, cancellationToken);

        var images = await database.Images
            .Where(i => i.OwnerType == ImageOwnerType.BoardingHouse && i.OwnerId == house.Id)
            .ToListAsync(cancellationToken);

        if (images.All(i => i.Id != imageId))
        {
            throw new NotFoundException(MessageKeys.Image.NotFound);
        }

        // Both sides of the flag move in one save, so no state exists where two images are
        // primary or none is.
        foreach (var image in images)
        {
            image.IsPrimary = image.Id == imageId;
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
