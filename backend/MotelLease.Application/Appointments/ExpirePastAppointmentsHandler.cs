using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Appointments;

/// <summary>
/// Closes viewing requests whose time has passed (docs/domain-rules.md §4). An unanswered request
/// becomes Expired; one the owner accepted becomes Completed, because the visit either happened or
/// nobody is going to answer for it now.
///
/// The rule lives here rather than in the background service so it can be run — and tested —
/// without a timer. The service in the Api layer only decides when.
/// </summary>
public sealed class ExpirePastAppointmentsHandler(IAppDbContext database, TimeProvider time)
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();

        var due = await database.Appointments
            .Where(a => a.AppointmentDate < now
                        && (a.Status == RequestStatus.Pending
                            || a.Status == RequestStatus.Accepted))
            .ToListAsync(cancellationToken);

        foreach (var appointment in due)
        {
            appointment.Status = appointment.Status == RequestStatus.Pending
                ? RequestStatus.Expired
                : RequestStatus.Completed;
        }

        await database.SaveChangesAsync(cancellationToken);

        return due.Count;
    }
}
