using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Infrastructure.Persistence;

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

        return services;
    }
}
