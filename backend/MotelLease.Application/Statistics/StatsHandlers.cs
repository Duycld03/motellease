using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Statistics.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Statistics;

public sealed class GetRevenueStatsHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<RevenueStatsResponse> HandleAsync(
        int? year,
        Guid? boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();
        var targetYear = year ?? time.GetUtcNow().Year;

        var query = database.PaymentBills
            .AsNoTracking()
            .Where(b => b.Room.BoardingHouse.OwnerUserId == ownerId &&
                        b.Status == BillStatus.Paid &&
                        b.Year == targetYear);

        if (boardingHouseId.HasValue)
        {
            query = query.Where(b => b.Room.BoardingHouseId == boardingHouseId.Value);
        }

        var paidBills = await query
            .Select(b => new
            {
                b.Month,
                b.TotalAmount,
                RentRevenue = b.RentAmount,
                UtilityAmount = b.ElectricityAmount + b.WaterAmount + b.AdditionalFeeTotal
            })
            .ToListAsync(cancellationToken);

        var monthlyMap = paidBills
            .GroupBy(b => b.Month)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Revenue = g.Sum(x => x.TotalAmount),
                    RentRevenue = g.Sum(x => x.RentRevenue),
                    UtilityRevenue = g.Sum(x => x.UtilityAmount),
                    Count = g.Count()
                });

        var breakdown = new List<MonthlyRevenueItem>();
        decimal totalRevenue = 0;
        decimal totalRentRevenue = 0;
        decimal totalUtilityRevenue = 0;
        int totalPaidBills = 0;

        for (int m = 1; m <= 12; m++)
        {
            if (monthlyMap.TryGetValue(m, out var data))
            {
                breakdown.Add(new MonthlyRevenueItem(m, data.Revenue, data.RentRevenue, data.UtilityRevenue, data.Count));
                totalRevenue += data.Revenue;
                totalRentRevenue += data.RentRevenue;
                totalUtilityRevenue += data.UtilityRevenue;
                totalPaidBills += data.Count;
            }
            else
            {
                breakdown.Add(new MonthlyRevenueItem(m, 0, 0, 0, 0));
            }
        }

        return new RevenueStatsResponse(
            targetYear,
            boardingHouseId,
            totalRevenue,
            totalRentRevenue,
            totalUtilityRevenue,
            totalPaidBills,
            breakdown);
    }
}

public sealed class GetRevenueYearsHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<RevenueYearsResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();
        var currentYear = time.GetUtcNow().Year;

        var billYears = await database.PaymentBills
            .AsNoTracking()
            .Where(b => b.Room.BoardingHouse.OwnerUserId == ownerId)
            .Select(b => b.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        var expenseYears = await database.BoardingHouseExpenses
            .AsNoTracking()
            .Where(e => e.BoardingHouse.OwnerUserId == ownerId)
            .Select(e => e.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allYears = billYears
            .Concat(expenseYears)
            .Append(currentYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        return new RevenueYearsResponse(allYears);
    }
}

public sealed class GetOccupancyStatsHandler(
    IAppDbContext database,
    ICurrentUser currentUser)
{
    public async Task<OccupancyStatsResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();

        var houses = await database.BoardingHouses
            .AsNoTracking()
            .Include(h => h.Rooms)
            .Where(h => h.OwnerUserId == ownerId)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);

        var houseItems = new List<HouseOccupancyItem>();
        int totalRooms = 0;
        int totalRented = 0;
        int totalReserved = 0;
        int totalVacant = 0;

        foreach (var house in houses)
        {
            var validRooms = house.Rooms.Where(r => !r.IsDeleted).ToList();
            var houseTotal = validRooms.Count;
            var houseRented = validRooms.Count(r => r.Status == RoomStatus.Occupied);
            var houseReserved = validRooms.Count(r => r.Status == RoomStatus.Reserved);
            var houseVacant = validRooms.Count(r => r.Status == RoomStatus.Available);
            var rate = houseTotal > 0 ? Math.Round((houseRented * 100.0) / houseTotal, 2) : 0;

            houseItems.Add(new HouseOccupancyItem(
                house.Id,
                house.Name,
                houseTotal,
                houseRented,
                houseReserved,
                houseVacant,
                rate));

            totalRooms += houseTotal;
            totalRented += houseRented;
            totalReserved += houseReserved;
            totalVacant += houseVacant;
        }

        var overallRate = totalRooms > 0 ? Math.Round((totalRented * 100.0) / totalRooms, 2) : 0;

        return new OccupancyStatsResponse(
            totalRooms,
            totalRented,
            totalReserved,
            totalVacant,
            overallRate,
            houseItems);
    }
}

public sealed class GetProfitStatsHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<ProfitStatsResponse> HandleAsync(
        int? year,
        Guid? boardingHouseId,
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();
        var targetYear = year ?? time.GetUtcNow().Year;

        // Revenue query
        var revQuery = database.PaymentBills
            .AsNoTracking()
            .Where(b => b.Room.BoardingHouse.OwnerUserId == ownerId &&
                        b.Status == BillStatus.Paid &&
                        b.Year == targetYear);

        if (boardingHouseId.HasValue)
        {
            revQuery = revQuery.Where(b => b.Room.BoardingHouseId == boardingHouseId.Value);
        }

        var revMonthly = await revQuery
            .GroupBy(b => b.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.Month, x => x.Total, cancellationToken);

        // Expense query
        var expQuery = database.BoardingHouseExpenses
            .AsNoTracking()
            .Where(e => e.BoardingHouse.OwnerUserId == ownerId &&
                        e.Year == targetYear);

        if (boardingHouseId.HasValue)
        {
            expQuery = expQuery.Where(e => e.BoardingHouseId == boardingHouseId.Value);
        }

        var expMonthly = await expQuery
            .GroupBy(e => e.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(x => x.TotalExpense) })
            .ToDictionaryAsync(x => x.Month, x => x.Total, cancellationToken);

        var breakdown = new List<MonthlyProfitItem>();
        decimal totalRevenue = 0;
        decimal totalExpense = 0;
        decimal totalNetProfit = 0;

        for (int m = 1; m <= 12; m++)
        {
            var rev = revMonthly.GetValueOrDefault(m, 0);
            var exp = expMonthly.GetValueOrDefault(m, 0);
            var net = rev - exp;

            breakdown.Add(new MonthlyProfitItem(m, rev, exp, net));
            totalRevenue += rev;
            totalExpense += exp;
            totalNetProfit += net;
        }

        return new ProfitStatsResponse(
            targetYear,
            boardingHouseId,
            totalRevenue,
            totalExpense,
            totalNetProfit,
            breakdown);
    }
}

public sealed class GetDashboardSummaryHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    TimeProvider time)
{
    public async Task<DashboardSummaryResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerId = currentUser.RequireUserId();
        var now = time.GetUtcNow();
        var currentMonth = now.Month;
        var currentYear = now.Year;

        var totalHouses = await database.BoardingHouses
            .AsNoTracking()
            .CountAsync(h => h.OwnerUserId == ownerId && !h.IsDeleted, cancellationToken);

        var rooms = await database.Rooms
            .AsNoTracking()
            .Where(r => r.BoardingHouse.OwnerUserId == ownerId && !r.IsDeleted)
            .Select(r => new { r.Status })
            .ToListAsync(cancellationToken);

        var totalRooms = rooms.Count;
        var occupiedRooms = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var vacantRooms = rooms.Count(r => r.Status == RoomStatus.Available);
        var occupancyRate = totalRooms > 0 ? Math.Round((occupiedRooms * 100.0) / totalRooms, 2) : 0;

        var activeLeases = await database.Leases
            .AsNoTracking()
            .CountAsync(l => l.Room.BoardingHouse.OwnerUserId == ownerId && l.Status == LeaseStatus.Active, cancellationToken);

        var pendingAppointments = await database.Appointments
            .AsNoTracking()
            .CountAsync(a => a.Room.BoardingHouse.OwnerUserId == ownerId && a.Status == RequestStatus.Pending, cancellationToken);

        var pendingMaintenance = await database.MaintenanceRequests
            .AsNoTracking()
            .CountAsync(m => m.Room.BoardingHouse.OwnerUserId == ownerId &&
                             (m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress),
                        cancellationToken);

        var unpaidBills = await database.PaymentBills
            .AsNoTracking()
            .Where(b => b.Room.BoardingHouse.OwnerUserId == ownerId &&
                        (b.Status == BillStatus.Issued || b.Status == BillStatus.Overdue))
            .Select(b => b.TotalAmount)
            .ToListAsync(cancellationToken);

        var unpaidBillsCount = unpaidBills.Count;
        var unpaidBillsAmount = unpaidBills.Sum();

        var revenueThisMonth = await database.PaymentBills
            .AsNoTracking()
            .Where(b => b.Room.BoardingHouse.OwnerUserId == ownerId &&
                        b.Status == BillStatus.Paid &&
                        b.Month == currentMonth &&
                        b.Year == currentYear)
            .SumAsync(b => (decimal?)b.TotalAmount, cancellationToken) ?? 0;

        var expensesThisMonth = await database.BoardingHouseExpenses
            .AsNoTracking()
            .Where(e => e.BoardingHouse.OwnerUserId == ownerId &&
                        e.Month == currentMonth &&
                        e.Year == currentYear)
            .SumAsync(e => (decimal?)e.TotalExpense, cancellationToken) ?? 0;

        var profitThisMonth = revenueThisMonth - expensesThisMonth;

        var availableBalance = await database.OwnerProfiles
            .AsNoTracking()
            .Where(op => op.UserId == ownerId)
            .Select(op => (decimal?)op.AvailableBalance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        return new DashboardSummaryResponse(
            totalHouses,
            totalRooms,
            occupiedRooms,
            vacantRooms,
            occupancyRate,
            activeLeases,
            pendingAppointments,
            pendingMaintenance,
            unpaidBillsCount,
            unpaidBillsAmount,
            revenueThisMonth,
            expensesThisMonth,
            profitThisMonth,
            availableBalance);
    }
}
