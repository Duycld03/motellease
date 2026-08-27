namespace MotelLease.Application.Expenses.Contracts;

public sealed record OtherExpenseItem(
    string FeeName,
    decimal FeeAmount);

public sealed record CreateExpenseRequest(
    int Month,
    int Year,
    decimal ElectricityOld,
    decimal ElectricityNew,
    decimal ElectricityQty,
    decimal ElectricityAmount,
    decimal WaterOld,
    decimal WaterNew,
    decimal WaterQty,
    decimal WaterAmount,
    List<OtherExpenseItem>? OtherExpenses = null);

public sealed record UpdateExpenseRequest(
    decimal ElectricityOld,
    decimal ElectricityNew,
    decimal ElectricityQty,
    decimal ElectricityAmount,
    decimal WaterOld,
    decimal WaterNew,
    decimal WaterQty,
    decimal WaterAmount,
    List<OtherExpenseItem>? OtherExpenses = null);

public sealed record ExpenseResponse(
    Guid Id,
    Guid BoardingHouseId,
    string BoardingHouseName,
    int Month,
    int Year,
    decimal ElectricityOld,
    decimal ElectricityNew,
    decimal ElectricityQty,
    decimal ElectricityAmount,
    decimal WaterOld,
    decimal WaterNew,
    decimal WaterQty,
    decimal WaterAmount,
    List<OtherExpenseItem> OtherExpenses,
    decimal OtherExpensesTotal,
    decimal TotalExpense,
    DateTimeOffset CreatedAt);
