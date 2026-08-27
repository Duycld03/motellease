using MotelLease.Application.Bills;

namespace MotelLease.Api.Jobs;

public sealed class BillReminderJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<BillReminderJob> logger) : BackgroundService
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
                    .GetRequiredService<SweepBillRemindersHandler>()
                    .HandleAsync(stoppingToken);

                if (count > 0)
                {
                    logger.LogInformation("Processed {Count} bill reminders / overdue transitions.", count);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The bill reminder sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
