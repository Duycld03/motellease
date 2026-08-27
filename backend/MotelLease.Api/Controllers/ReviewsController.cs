using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Reviews;
using MotelLease.Application.Reviews.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReviewsController : ControllerBase
{
    [HttpPost("reviews")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<ReviewResponse>> Create(
        [FromBody] CreateReviewRequest request,
        [FromServices] CreateReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMyReviews), new { id = result.Id }, result);
    }

    [HttpPut("reviews/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> Update(
        Guid id,
        [FromBody] UpdateReviewRequest request,
        [FromServices] UpdateReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteReviewHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("reviews/{id:guid}/reply")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<ReviewReplyResponse>> Reply(
        Guid id,
        [FromBody] ReplyReviewRequest request,
        [FromServices] ReplyReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("reviews/{id:guid}/reply/{replyId:guid}")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<ReviewReplyResponse>> UpdateReply(
        Guid id,
        Guid replyId,
        [FromBody] ReplyReviewRequest request,
        [FromServices] UpdateReviewReplyHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, replyId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("reviews/{id:guid}/reply/{replyId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReply(
        Guid id,
        Guid replyId,
        [FromServices] DeleteReviewReplyHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, replyId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/reviews")]
    [Authorize(Roles = "Tenant")]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListUserReviewsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my/reviews")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetManagedPropertyReviews(
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListPropertyReviewsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(boardingHouseId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
