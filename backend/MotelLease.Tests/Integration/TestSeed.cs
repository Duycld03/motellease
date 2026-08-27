using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Rows that belong to feature groups the API does not expose yet — staff accounts, admin listing
/// approval — written straight to the database. Anything an endpoint can already do is done through
/// the endpoint instead, so the tests keep exercising the real code.
/// </summary>
internal static class TestSeed
{
    internal static async Task<StaffAccount> AssignStaffAsync(
        this MotelLeaseAppFactory app,
        HttpClient client,
        Guid boardingHouseId,
        Guid ownerUserId)
    {
        var email = $"staff-{Guid.NewGuid():N}@example.com";
        Guid staffUserId;

        using (var scope = app.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var staff = new User
            {
                Username = email,
                Email = email,
                PasswordHash = passwordHasher.Hash(ApiRequests.Password),
                FullName = "Tran Thi B",
                Role = UserRole.Staff,
                EmailConfirmed = true
            };

            database.Users.Add(staff);

            database.StaffProfiles.Add(new StaffProfile
            {
                UserId = staff.Id,
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedByUserId = ownerUserId
            });

            database.StaffAssignments.Add(new StaffAssignment
            {
                BoardingHouseId = boardingHouseId,
                StaffUserId = staff.Id,
                AssignedByUserId = ownerUserId,
                AssignedAt = DateTimeOffset.UtcNow
            });

            await database.SaveChangesAsync();

            staffUserId = staff.Id;
        }

        return new StaffAccount(staffUserId, await client.LoginAsync(email));
    }

    internal static async Task UnassignStaffAsync(
        this MotelLeaseAppFactory app,
        Guid staffUserId)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        foreach (var assignment in await database.StaffAssignments
                     .Where(a => a.StaffUserId == staffUserId && a.UnassignedAt == null)
                     .ToListAsync())
        {
            assignment.UnassignedAt = DateTimeOffset.UtcNow;
        }

        await database.SaveChangesAsync();
    }

    /// <summary>Stands in for the admin approval endpoint, which is a later feature group.</summary>
    internal static async Task PublishAsync(this MotelLeaseAppFactory app, Guid boardingHouseId)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MotelLeaseDbContext>();

        var house = await database.BoardingHouses.FirstAsync(b => b.Id == boardingHouseId);

        house.ListingStatus = ListingStatus.Published;

        await database.SaveChangesAsync();
    }
}

internal sealed record StaffAccount(Guid UserId, string AccessToken);
