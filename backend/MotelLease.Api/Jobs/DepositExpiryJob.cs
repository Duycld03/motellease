using MotelLease.Application.Deposits;

namespace MotelLease.Api.Jobs;

/// <summary>
/// Runs the deposit sweep on a timer (docs/domain-rules.md §8). Every 15 minutes, because the
/// payment deadline is the tenant's promise and the room stays off the market until it is released.
/// Registered explicitly in Program.cs and nowhere else.
///
/// The rule itself is <see cref="ExpireOverdueDepositsHandler"/>. This class only owns the clock
/// and the scope.
/// </summary>
public sealed class DepositExpiryJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<DepositExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, time);

        // Swept once at startup as well: a process that was down over a scheduled tick would
        // otherwise keep those rooms held for another full period.
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var expired = await scope.ServiceProvider
                    .GetRequiredService<ExpireOverdueDepositsHandler>()
                    .HandleAsync(stoppingToken);

                if (expired > 0)
                {
                    logger.LogInformation("Released {Count} unpaid deposit requests.", expired);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // A failed sweep must not end the service; the next tick tries again.
                logger.LogError(exception, "The deposit sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
