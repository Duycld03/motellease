using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.BoardingHouses;
using MotelLease.Application.BoardingHouses.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Security;

namespace MotelLease.Api.Controllers;

/// <summary>
/// The properties the caller runs. One set of endpoints for owners and staff: the role opens the
/// door, the <see cref="BoardingHouseAccess"/> check inside each handler decides which rows are
/// reachable (docs/domain-rules.md §6). The owner-only actions add the stricter policy.
/// </summary>
[ApiController]
[Route("api/v1/my/boarding-houses")]
[Authorize(Policy = AuthPolicies.RequireStaffOrOwner)]
public sealed class MyBoardingHousesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<BoardingHouseSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<BoardingHouseSummaryResponse>>> List(
        [FromServices] ListMyBoardingHousesHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BoardingHouseDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardingHouseDetailResponse>> Get(
        Guid id,
        [FromServices] GetBoardingHouseHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireOwner)]
    [ProducesResponseType(typeof(BoardingHouseDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BoardingHouseDetailResponse>> Create(
        SaveBoardingHouseRequest request,
        [FromServices] CreateBoardingHouseHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BoardingHouseDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardingHouseDetailResponse>> Update(
        Guid id,
        SaveBoardingHouseRequest request,
        [FromServices] UpdateBoardingHouseHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.RequireOwner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteBoardingHouseHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>Queues the listing for admin review: Draft or Rejected → PendingReview.</summary>
    [HttpPut("{id:guid}/submit-review")]
    [Authorize(Policy = AuthPolicies.RequireOwner)]
    [ProducesResponseType(typeof(BoardingHouseDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardingHouseDetailResponse>> SubmitForReview(
        Guid id,
        [FromServices] SubmitBoardingHouseForReviewHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPut("{id:guid}/utility-prices")]
    [Authorize(Policy = AuthPolicies.RequireOwner)]
    [ProducesResponseType(typeof(BoardingHouseDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardingHouseDetailResponse>> UpdateUtilityPrices(
        Guid id,
        UpdateUtilityPricesRequest request,
        [FromServices] UpdateUtilityPricesHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, request, cancellationToken));

    /// <summary>
    /// Multipart upload. The size cap is enforced again in the handler against the declared
    /// length, because a form limit says nothing about what the storage provider receives.
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes)]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ImageResponse>> AddImage(
        Guid id,
        IFormFile file,
        [FromServices] AddBoardingHouseImageHandler handler,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();

        var image = await handler.HandleAsync(
            id,
            new AddImageRequest(content, file.FileName, file.ContentType, file.Length),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id }, image);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(
        Guid id,
        Guid imageId,
        [FromServices] DeleteBoardingHouseImageHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, imageId, cancellationToken);

        return NoContent();
    }

    /// <summary>The cover picture. Exactly one image is primary while the listing has any.</summary>
    [HttpPut("{id:guid}/images/{imageId:guid}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPrimaryImage(
        Guid id,
        Guid imageId,
        [FromServices] SetPrimaryBoardingHouseImageHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, imageId, cancellationToken);

        return NoContent();
    }
}
