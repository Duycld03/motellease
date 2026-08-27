using MotelLease.Application.Leases;

namespace MotelLease.Api.Jobs;

public sealed class LeaseExpiryJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<LeaseExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, time);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var count = await scope.ServiceProvider
                    .GetRequiredService<SweepLeaseExpiryHandler>()
                    .HandleAsync(stoppingToken);

                if (count > 0)
                {
                    logger.LogInformation("Processed {Count} expiring / ended leases.", count);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The lease expiry sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
