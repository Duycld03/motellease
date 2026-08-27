using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Infrastructure.Caching;
using MotelLease.Infrastructure.Email;
using MotelLease.Infrastructure.Localization;
using MotelLease.Infrastructure.Payments;
using MotelLease.Infrastructure.Persistence;
using MotelLease.Infrastructure.Security;
using MotelLease.Infrastructure.Storage;

namespace MotelLease.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is missing. Set it in appsettings.Development.json " +
                "or as ConnectionStrings__Default in the environment.");

        services.AddDbContext<MotelLeaseDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Required for the geography column on BoardingHouses.
                npgsql.UseNetTopologySuite();
                npgsql.MigrationsAssembly(typeof(MotelLeaseDbContext).Assembly.FullName);
            }));

        // Handlers depend on the interface; the DbContext is the implementation. Same scoped
        // instance either way, so a handler and a controller share one change tracker.
        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<MotelLeaseDbContext>());

        services.TryAddSingleton(TimeProvider.System);

        AddSecurity(services, configuration);
        AddOtp(services, configuration);
        AddEmail(services, configuration);
        AddStorage(services, configuration);
        AddPayments(services, configuration);

        services.AddSingleton<ILocalizer, JsonLocalizer>();
        services.AddSingleton<IBillPdfGenerator, Documents.QuestPdfBillGenerator>();

        return services;
    }

    private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            // Fail at startup, not at the first login: a missing signing key is a deployment
            // mistake and must be loud.
            .ValidateOnStart();

        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
    }

    private static void AddOtp(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // In-memory today. Codes live for minutes, so losing them on restart costs a resend;
        // moving to Redis is a one-line change here and nothing else.
        services.AddDistributedMemoryCache();

        services.AddSingleton<IOtpService, DistributedOtpService>();
        services.AddSingleton<IVerifiedEmailStore, DistributedVerifiedEmailStore>();
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SmtpOptions.SectionName);

        services.AddOptions<SmtpOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var smtp = section.Get<SmtpOptions>() ?? new SmtpOptions();

        // Without a host there is nothing to connect to; the OTP then goes to the log so the
        // flows stay testable locally.
        if (smtp.IsConfigured)
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
    }

    private static void AddStorage(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(CloudinaryOptions.SectionName);

        services.AddOptions<CloudinaryOptions>().Bind(section);

        var cloudinary = section.Get<CloudinaryOptions>() ?? new CloudinaryOptions();

        if (cloudinary.IsConfigured)
        {
            services.AddSingleton<IImageStorage, CloudinaryImageStorage>();
        }
        else
        {
            services.AddSingleton<IImageStorage, UnconfiguredImageStorage>();
        }
    }

    private static void AddPayments(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppUrlOptions>()
            .Bind(configuration.GetSection(AppUrlOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var section = configuration.GetSection(VnPayOptions.SectionName);

        services.AddOptions<VnPayOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Registered only when there is a merchant code and a secret to sign with. Unregistered, a
        // checkout is refused with "provider not available" instead of building a URL the gateway
        // would reject — and no branch of the payment code can run unsigned.
        if ((section.Get<VnPayOptions>() ?? new VnPayOptions()).IsConfigured)
        {
            services.AddSingleton<IPaymentGateway, VnPayGateway>();
        }

        var momo = configuration.GetSection(MoMoOptions.SectionName);

        services.AddOptions<MoMoOptions>()
            .Bind(momo)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The same rule, and a typed HttpClient because MoMo has to be asked for a payment URL
        // rather than handed one.
        if ((momo.Get<MoMoOptions>() ?? new MoMoOptions()).IsConfigured)
        {
            services.AddHttpClient(MoMoGateway.HttpClientName);
            services.AddSingleton<IPaymentGateway, MoMoGateway>();
        }
    }
}
