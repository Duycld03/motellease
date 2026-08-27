using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Accounts;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Auth.Contracts;

namespace MotelLease.Api.Controllers;

/// <summary>
/// The caller's own account. Every action is scoped to the token's subject, so there is no id
/// in any route here and no way to reach someone else's profile.
/// </summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> GetProfile(
        [FromServices] GetProfileHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        UpdateProfileRequest request,
        [FromServices] UpdateProfileHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    /// <summary>
    /// Multipart upload. The size cap is enforced again in the handler against the declared
    /// length, because a form limit alone says nothing about what the storage provider gets.
    /// </summary>
    [HttpPut("avatar")]
    [RequestSizeLimit(UpdateAvatarHandler.MaxBytes)]
    [ProducesResponseType(typeof(AvatarResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvatarResponse>> UpdateAvatar(
        IFormFile file,
        [FromServices] UpdateAvatarHandler handler,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();

        var request = new UpdateAvatarRequest(
            content,
            file.FileName,
            file.ContentType,
            file.Length);

        return Ok(await handler.HandleAsync(request, cancellationToken));
    }

    /// <summary>
    /// Sets the language used for emails and notifications. The UI language of the current
    /// request still comes from Accept-Language.
    /// </summary>
    [HttpPut("language")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLanguage(
        UpdateLanguageRequest request,
        [FromServices] UpdateLanguageHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Sends a code to the new address; the change only lands after verify-otp.</summary>
    [HttpPost("email/send-otp")]
    [ProducesResponseType(typeof(OtpSentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OtpSentResponse>> SendEmailChangeOtp(
        SendEmailChangeOtpRequest request,
        [FromServices] SendEmailChangeOtpHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpPost("email/verify-otp")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmailChangeOtp(
        VerifyEmailChangeOtpRequest request,
        [FromServices] VerifyEmailChangeOtpHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Live sessions, with the caller's own marked so the UI can label it.</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetSessions(
        [FromServices] GetSessionsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    /// <summary>Signs one device out. Only the caller's own sessions are reachable.</summary>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(
        Guid id,
        [FromServices] RevokeSessionHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);

        return NoContent();
    }
}
