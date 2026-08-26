using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotelLease.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces the Revenue table and the totalRooms/availableRooms/priceRange caches of the
    /// original project (docs/erd.md §7). Both views carry a unique index because
    /// REFRESH MATERIALIZED VIEW CONCURRENTLY fails without one.
    /// </summary>
    public partial class ReportingViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW vw_monthly_revenue AS
                WITH paid_bills AS (
                    SELECT b."Id", b."Year", b."Month", b."TotalAmount", r."BoardingHouseId"
                    FROM "PaymentBills" b
                    JOIN "Rooms" r ON r."Id" = b."RoomId"
                    WHERE b."Status" = 'Paid'
                ),
                succeeded_txns AS (
                    SELECT "PaymentBillId", COUNT(*) AS "Count"
                    FROM "PaymentTransactions"
                    WHERE "Status" = 'Succeeded' AND "PaymentBillId" IS NOT NULL
                    GROUP BY "PaymentBillId"
                )
                SELECT p."BoardingHouseId",
                       p."Year",
                       p."Month",
                       SUM(p."TotalAmount") AS "TotalRevenue",
                       COALESCE(SUM(t."Count"), 0)::int AS "TransactionCount",
                       COUNT(*)::int AS "PaidBillCount"
                FROM paid_bills p
                LEFT JOIN succeeded_txns t ON t."PaymentBillId" = p."Id"
                GROUP BY p."BoardingHouseId", p."Year", p."Month";
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_vw_monthly_revenue_House_Year_Month"
                    ON vw_monthly_revenue ("BoardingHouseId", "Year", "Month");
                """);

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW vw_room_occupancy AS
                SELECT b."Id" AS "BoardingHouseId",
                       COUNT(r."Id")::int AS "TotalRooms",
                       COUNT(r."Id") FILTER (WHERE r."Status" = 'Available')::int AS "AvailableRooms",
                       COUNT(r."Id") FILTER (WHERE r."Status" = 'Reserved')::int AS "ReservedRooms",
                       COUNT(r."Id") FILTER (WHERE r."Status" = 'Occupied')::int AS "OccupiedRooms",
                       COUNT(r."Id") FILTER (WHERE r."Status" = 'Maintenance')::int AS "MaintenanceRooms",
                       MIN(t."Price") AS "MinPrice",
                       MAX(t."Price") AS "MaxPrice"
                FROM "BoardingHouses" b
                LEFT JOIN "Rooms" r
                       ON r."BoardingHouseId" = b."Id" AND r."IsDeleted" = false
                LEFT JOIN "RoomTypes" t
                       ON t."Id" = r."RoomTypeId" AND t."IsDeleted" = false
                WHERE b."IsDeleted" = false
                GROUP BY b."Id";
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_vw_room_occupancy_House"
                    ON vw_room_occupancy ("BoardingHouseId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS vw_room_occupancy;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS vw_monthly_revenue;");
        }
    }
}
