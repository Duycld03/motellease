namespace MotelLease.Application.Statistics.Contracts;

public sealed record MonthlyRevenueItem(
    int Month,
    decimal Revenue,
    decimal RentRevenue,
    decimal UtilityRevenue,
    int PaidBillsCount);

public sealed record RevenueStatsResponse(
    int Year,
    Guid? BoardingHouseId,
    decimal TotalRevenue,
    decimal TotalRentRevenue,
    decimal TotalUtilityRevenue,
    int TotalPaidBills,
    List<MonthlyRevenueItem> MonthlyBreakdown);

public sealed record RevenueYearsResponse(
    List<int> Years);

public sealed record HouseOccupancyItem(
    Guid BoardingHouseId,
    string BoardingHouseName,
    int TotalRooms,
    int RentedRooms,
    int ReservedRooms,
    int VacantRooms,
    double OccupancyRate);

public sealed record OccupancyStatsResponse(
    int TotalRooms,
    int RentedRooms,
    int ReservedRooms,
    int VacantRooms,
    double OverallOccupancyRate,
    List<HouseOccupancyItem> Houses);

public sealed record MonthlyProfitItem(
    int Month,
    decimal Revenue,
    decimal Expense,
    decimal NetProfit);

public sealed record ProfitStatsResponse(
    int Year,
    Guid? BoardingHouseId,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal TotalNetProfit,
    List<MonthlyProfitItem> MonthlyBreakdown);

public sealed record DashboardSummaryResponse(
    int TotalBoardingHouses,
    int TotalRooms,
    int OccupiedRooms,
    int VacantRooms,
    double OccupancyRate,
    int ActiveLeases,
    int PendingAppointments,
    int PendingMaintenanceRequests,
    int UnpaidBillsCount,
    decimal UnpaidBillsAmount,
    decimal RevenueThisMonth,
    decimal ExpensesThisMonth,
    decimal ProfitThisMonth,
    decimal AvailableBalance);
