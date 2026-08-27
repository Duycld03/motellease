using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Rooms;

/// <summary>
/// Which <see cref="RoomStatus"/> changes a person is allowed to make.
/// <see cref="RoomStatus.Occupied"/> and <see cref="RoomStatus.Reserved"/> are derived from
/// lease and deposit rows (docs/domain-rules.md §9.3): setting either by hand would make the
/// column a second source of truth, free to drift from the rows it is supposed to summarise.
/// So the only manual move is between an empty room and one taken out of service.
/// </summary>
public static class RoomStatusPolicy
{
    public static bool IsManuallySettable(RoomStatus target) =>
        target is RoomStatus.Available or RoomStatus.Maintenance;

    /// <summary>
    /// A room that is spoken for cannot be moved out of that state by editing it. The lease
    /// has to end or the deposit has to be released first, and the status follows from that.
    /// </summary>
    public static bool CanBeChangedFrom(RoomStatus current) =>
        current is RoomStatus.Available or RoomStatus.Maintenance;

    /// <summary>
    /// The status implied by the rows that commit a room (docs/domain-rules.md §9.3). A lease wins
    /// over a deposit: somebody living there outranks somebody holding it. The caller supplies the
    /// two facts so the rule stays free of I/O and can be tested without a database.
    /// </summary>
    public static RoomStatus DeriveFromCommitments(bool hasLiveLease, bool hasHoldingDeposit) =>
        (hasLiveLease, hasHoldingDeposit) switch
        {
            (true, _) => RoomStatus.Occupied,
            (false, true) => RoomStatus.Reserved,
            _ => RoomStatus.Available
        };
}
