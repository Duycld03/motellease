using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Images;
using MotelLease.Application.Images.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/images")]
[Authorize]
public sealed class ImagesController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UploadImageResponse>> Upload(
        IFormFile file,
        [FromServices] UploadImageHandler handler,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var request = new UploadFileRequest(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        var result = await handler.HandleAsync(request, cancellationToken: cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{*publicId}")]
    public async Task<IActionResult> Delete(
        string publicId,
        [FromServices] DeleteImageHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(publicId, cancellationToken);
        return NoContent();
    }
}
