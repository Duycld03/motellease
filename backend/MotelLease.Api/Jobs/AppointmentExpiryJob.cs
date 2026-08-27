using MotelLease.Application.Appointments;

namespace MotelLease.Api.Jobs;

/// <summary>
/// Runs the appointment sweep on a timer (docs/domain-rules.md §8). Registered explicitly in
/// Program.cs and nowhere else: a job declared inside an entity or a configuration runs whenever
/// that type is loaded, including during tests, and nobody controls its lifetime.
///
/// The rule itself is <see cref="ExpirePastAppointmentsHandler"/>. This class only owns the clock
/// and the scope.
/// </summary>
public sealed class AppointmentExpiryJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<AppointmentExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, time);

        // Swept once at startup as well: a process that was down over a scheduled tick would
        // otherwise leave those requests open for another full period.
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var expired = await scope.ServiceProvider
                    .GetRequiredService<ExpirePastAppointmentsHandler>()
                    .HandleAsync(stoppingToken);

                if (expired > 0)
                {
                    logger.LogInformation("Closed {Count} past viewing appointments.", expired);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // A failed sweep must not end the service; the next tick tries again.
                logger.LogError(exception, "The appointment sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
