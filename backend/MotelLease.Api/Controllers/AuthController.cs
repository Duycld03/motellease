using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MotelLease.Api.RateLimiting;
using MotelLease.Application.Auth;
using MotelLease.Application.Auth.Contracts;

namespace MotelLease.Api.Controllers;

/// <summary>
/// Anonymous entry points plus the two that need a token (logout, change password). Every
/// action is a thin call into one handler — the rules live in Application.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Step 1 of registration: proves the visitor can read the inbox. Throttled per address
    /// by the OTP service and per caller by the rate limiter.
    /// </summary>
    [HttpPost("register/send-otp")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Otp)]
    [ProducesResponseType(typeof(OtpSentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OtpSentResponse>> SendRegistrationOtp(
        SendRegistrationOtpRequest request,
        [FromServices] SendRegistrationOtpHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    /// <summary>Step 2: marks the address verified for a short window.</summary>
    [HttpPost("register/verify-otp")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Otp)]
    [ProducesResponseType(typeof(OtpVerifiedResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OtpVerifiedResponse>> VerifyRegistrationOtp(
        VerifyRegistrationOtpRequest request,
        [FromServices] VerifyRegistrationOtpHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    /// <summary>Step 3: creates the account and signs it in straight away.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> Register(
        RegisterRequest request,
        [FromServices] RegisterHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> Login(
        LoginRequest request,
        [FromServices] LoginHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    [HttpPost("login/google")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> LoginWithGoogle(
        GoogleLoginRequest request,
        [FromServices] LoginWithGoogleHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    /// <summary>
    /// Rotation: the presented token is revoked and a new pair issued. Presenting an already
    /// revoked token kills every session for that user — see RefreshTokenHandler.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(
        RefreshTokenRequest request,
        [FromServices] RefreshTokenHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    /// <summary>Revokes the presented refresh token only; other devices stay signed in.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        [FromServices] LogoutHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Answers the same way for a known and an unknown address, so the endpoint cannot be
    /// used to find out who has an account.
    /// </summary>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Otp)]
    [ProducesResponseType(typeof(OtpSentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OtpSentResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        [FromServices] ForgotPasswordHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(request, cancellationToken));

    /// <summary>Sets a new password and signs every device out.</summary>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        [FromServices] ResetPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Keeps the calling session alive and revokes the others.</summary>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        [FromServices] ChangePasswordHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);

        return NoContent();
    }
}
