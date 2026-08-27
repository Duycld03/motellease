using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Common.Security;

/// <summary>
/// Layer two of authorization (docs/domain-rules.md §6): the role in the token says the caller
/// is an owner or a staff member, it does not say which boarding houses they are responsible
/// for. Every handler that takes a boarding house id goes through here first.
/// </summary>
public sealed class BoardingHouseAccess(IAppDbContext database, ICurrentUser currentUser)
{
    /// <summary>
    /// The house, tracked so the caller can mutate it. Staff pass only while their
    /// <see cref="StaffAssignment"/> is live — a revoked assignment ends access immediately,
    /// which is what §9.12 requires.
    /// </summary>
    public async Task<BoardingHouse> RequireStaffOrOwnerAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var house = await LoadAsync(boardingHouseId, cancellationToken);

        if (house.OwnerUserId == userId)
        {
            return house;
        }

        var assigned = await database.StaffAssignments.AnyAsync(
            a => a.BoardingHouseId == house.Id
                 && a.StaffUserId == userId
                 && a.UnassignedAt == null,
            cancellationToken);

        return assigned
            ? house
            : throw new ForbiddenException(MessageKeys.BoardingHouse.NotYours);
    }

    /// <summary>
    /// For decisions that belong to the person who owns the property rather than to whoever
    /// runs it day to day: deleting it, pricing utilities, publishing the listing.
    /// </summary>
    public async Task<BoardingHouse> RequireOwnerAsync(
        Guid boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var house = await LoadAsync(boardingHouseId, cancellationToken);

        return house.OwnerUserId == userId
            ? house
            : throw new ForbiddenException(MessageKeys.BoardingHouse.NotYours);
    }

    private async Task<BoardingHouse> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await database.BoardingHouses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
        ?? throw new NotFoundException(MessageKeys.BoardingHouse.NotFound);

    /// <summary>
    /// The properties the caller runs, as a query the list endpoints compose onto. An owner runs
    /// the ones they own, a staff member the ones they are currently assigned to.
    /// </summary>
    public IQueryable<BoardingHouse> Managed()
    {
        var userId = currentUser.RequireUserId();

        return currentUser.Role == UserRole.Staff
            ? database.BoardingHouses.Where(b => database.StaffAssignments.Any(
                a => a.BoardingHouseId == b.Id
                     && a.StaffUserId == userId
                     && a.UnassignedAt == null))
            : database.BoardingHouses.Where(b => b.OwnerUserId == userId);
    }
}
