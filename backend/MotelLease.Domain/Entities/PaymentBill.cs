using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

/// <summary>
/// A monthly invoice. Quantities, unit prices and amounts are all stored, not computed:
/// the bill is a historical document and must not follow later price changes
/// (docs/domain-rules.md §3).
/// </summary>
public class PaymentBill : Entity
{
    public Guid LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }

    public decimal RentAmount { get; set; }

    public decimal ElectricityOld { get; set; }
    public decimal ElectricityNew { get; set; }
    public decimal ElectricityQty { get; set; }
    public decimal ElectricityUnitPrice { get; set; }
    public decimal ElectricityAmount { get; set; }

    public decimal WaterOld { get; set; }
    public decimal WaterNew { get; set; }
    public decimal WaterQty { get; set; }
    public decimal WaterUnitPrice { get; set; }
    public decimal WaterAmount { get; set; }

    public decimal AdditionalFeeTotal { get; set; }
    public decimal TotalAmount { get; set; }

    public BillStatus Status { get; set; } = BillStatus.Draft;

    public DateTimeOffset? IssuedAt { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    public ICollection<RoomAdditionalFee> AdditionalFees { get; set; } = [];
}

/// <summary>
/// An extra charge recorded against a room for a given month. PaymentBillId stays null
/// until the bill for that month is issued and absorbs the fee.
/// </summary>
public class RoomAdditionalFee : Entity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid? PaymentBillId { get; set; }
    public PaymentBill? PaymentBill { get; set; }

    public string FeeName { get; set; } = null!;
    public decimal FeeAmount { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }
}

/// <summary>
/// Monthly utility and running costs of a whole house, entered by the owner. Kept apart
/// from PaymentBills: this is what the owner pays out, not what a tenant owes.
/// </summary>
public class BoardingHouseExpense : Entity
{
    public Guid BoardingHouseId { get; set; }
    public BoardingHouse BoardingHouse { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }

    public decimal ElectricityOld { get; set; }
    public decimal ElectricityNew { get; set; }
    public decimal ElectricityQty { get; set; }
    public decimal ElectricityAmount { get; set; }

    public decimal WaterOld { get; set; }
    public decimal WaterNew { get; set; }
    public decimal WaterQty { get; set; }
    public decimal WaterAmount { get; set; }

    /// <summary>
    /// <c>[{"feeName": ..., "feeAmount": ...}]</c>. Stored as jsonb rather than a child
    /// table because it is only ever displayed and summed, never filtered on (docs/erd.md §4).
    /// </summary>
    public string OtherExpenses { get; set; } = "[]";

    public decimal OtherExpensesTotal { get; set; }
    public decimal TotalExpense { get; set; }
}
