using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Expenses.Contracts;
using MotelLease.Domain.Entities;

namespace MotelLease.Application.Expenses;

public static class ExpenseMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static List<OtherExpenseItem> ParseOtherExpenses(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<OtherExpenseItem>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeOtherExpenses(List<OtherExpenseItem>? items)
    {
        return JsonSerializer.Serialize(items ?? [], JsonOptions);
    }

    public static ExpenseResponse ToResponse(BoardingHouseExpense expense)
    {
        return new ExpenseResponse(
            expense.Id,
            expense.BoardingHouseId,
            expense.BoardingHouse?.Name ?? string.Empty,
            expense.Month,
            expense.Year,
            expense.ElectricityOld,
            expense.ElectricityNew,
            expense.ElectricityQty,
            expense.ElectricityAmount,
            expense.WaterOld,
            expense.WaterNew,
            expense.WaterQty,
            expense.WaterAmount,
            ParseOtherExpenses(expense.OtherExpenses),
            expense.OtherExpensesTotal,
            expense.TotalExpense,
            expense.CreatedAt);
    }
}

public sealed class ListExpensesHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<PagedResponse<ExpenseResponse>> HandleAsync(
        Guid houseId,
        int? year,
        int? month,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        await access.RequireStaffOrOwnerAsync(houseId, cancellationToken);

        var query = database.BoardingHouseExpenses
            .AsNoTracking()
            .Include(e => e.BoardingHouse)
            .Where(e => e.BoardingHouseId == houseId)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(e => e.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(e => e.Month == month.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = items.Select(ExpenseMapping.ToResponse).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResponse<ExpenseResponse>(responses, page, pageSize, total, totalPages);
    }
}

public sealed class CreateExpenseHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<ExpenseResponse> HandleAsync(
        Guid houseId,
        CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var house = await access.RequireOwnerAsync(houseId, cancellationToken);

        var exists = await database.BoardingHouseExpenses
            .AnyAsync(e => e.BoardingHouseId == houseId && e.Month == request.Month && e.Year == request.Year, cancellationToken);

        if (exists)
        {
            throw new ConflictException(MessageKeys.Expense.AlreadyExists);
        }

        var otherExpenses = request.OtherExpenses ?? [];
        var otherTotal = otherExpenses.Sum(x => x.FeeAmount);
        var totalExpense = request.ElectricityAmount + request.WaterAmount + otherTotal;

        var expense = new BoardingHouseExpense
        {
            BoardingHouseId = houseId,
            BoardingHouse = house,
            Month = request.Month,
            Year = request.Year,
            ElectricityOld = request.ElectricityOld,
            ElectricityNew = request.ElectricityNew,
            ElectricityQty = request.ElectricityQty,
            ElectricityAmount = request.ElectricityAmount,
            WaterOld = request.WaterOld,
            WaterNew = request.WaterNew,
            WaterQty = request.WaterQty,
            WaterAmount = request.WaterAmount,
            OtherExpenses = ExpenseMapping.SerializeOtherExpenses(otherExpenses),
            OtherExpensesTotal = otherTotal,
            TotalExpense = totalExpense
        };

        database.BoardingHouseExpenses.Add(expense);
        await database.SaveChangesAsync(cancellationToken);

        return ExpenseMapping.ToResponse(expense);
    }
}

public sealed class GetExpenseHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<ExpenseResponse> HandleAsync(
        Guid houseId,
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        await access.RequireStaffOrOwnerAsync(houseId, cancellationToken);

        var expense = await database.BoardingHouseExpenses
            .AsNoTracking()
            .Include(e => e.BoardingHouse)
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.BoardingHouseId == houseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Expense.NotFound);

        return ExpenseMapping.ToResponse(expense);
    }
}

public sealed class UpdateExpenseHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task<ExpenseResponse> HandleAsync(
        Guid houseId,
        Guid expenseId,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        await access.RequireOwnerAsync(houseId, cancellationToken);

        var expense = await database.BoardingHouseExpenses
            .Include(e => e.BoardingHouse)
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.BoardingHouseId == houseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Expense.NotFound);

        var otherExpenses = request.OtherExpenses ?? [];
        var otherTotal = otherExpenses.Sum(x => x.FeeAmount);
        var totalExpense = request.ElectricityAmount + request.WaterAmount + otherTotal;

        expense.ElectricityOld = request.ElectricityOld;
        expense.ElectricityNew = request.ElectricityNew;
        expense.ElectricityQty = request.ElectricityQty;
        expense.ElectricityAmount = request.ElectricityAmount;
        expense.WaterOld = request.WaterOld;
        expense.WaterNew = request.WaterNew;
        expense.WaterQty = request.WaterQty;
        expense.WaterAmount = request.WaterAmount;
        expense.OtherExpenses = ExpenseMapping.SerializeOtherExpenses(otherExpenses);
        expense.OtherExpensesTotal = otherTotal;
        expense.TotalExpense = totalExpense;

        await database.SaveChangesAsync(cancellationToken);

        return ExpenseMapping.ToResponse(expense);
    }
}

public sealed class DeleteExpenseHandler(
    IAppDbContext database,
    BoardingHouseAccess access)
{
    public async Task HandleAsync(
        Guid houseId,
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        await access.RequireOwnerAsync(houseId, cancellationToken);

        var expense = await database.BoardingHouseExpenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.BoardingHouseId == houseId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Expense.NotFound);

        database.BoardingHouseExpenses.Remove(expense);
        await database.SaveChangesAsync(cancellationToken);
    }
}
