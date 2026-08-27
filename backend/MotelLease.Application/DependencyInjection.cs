using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Accounts;
using MotelLease.Application.Auth;

namespace MotelLease.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Handlers are scoped: each one holds the request's IAppDbContext and ICurrentUser.
    /// Registered explicitly rather than by assembly scan, so an unreferenced handler shows
    /// up as a missing registration instead of silently resolving.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterHandler>(ServiceLifetime.Singleton);

        services.AddScoped<SessionIssuer>();
        services.AddScoped<OtpDispatcher>();
        services.AddSingleton(VerifiedEmailWindow.Default);

        services.AddScoped<SendRegistrationOtpHandler>();
        services.AddScoped<VerifyRegistrationOtpHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<LoginWithGoogleHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();

        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<UpdateLanguageHandler>();
        services.AddScoped<UpdateAvatarHandler>();
        services.AddScoped<SendEmailChangeOtpHandler>();
        services.AddScoped<VerifyEmailChangeOtpHandler>();
        services.AddScoped<GetSessionsHandler>();
        services.AddScoped<RevokeSessionHandler>();

        return services;
    }
}
