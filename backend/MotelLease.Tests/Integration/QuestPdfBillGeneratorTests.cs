using System.Globalization;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Documents;
using Xunit;

namespace MotelLease.Tests.Integration;

public sealed class QuestPdfBillGeneratorTests
{
    [Fact]
    public void Generate_ProducesValidPdfBytes_ForVietnameseAndEnglish()
    {
        var generator = new QuestPdfBillGenerator();
        var bill = new BillResponse(
            Id: Guid.NewGuid(),
            LeaseId: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            RoomNumber: "P.201",
            BoardingHouseId: Guid.NewGuid(),
            BoardingHouseName: "Khu nhà trọ an ninh Trần Đại Nghĩa - Cơ sở 1",
            Month: 7,
            Year: 2026,
            RentAmount: 3800000m,
            ElectricityOld: 711.0m,
            ElectricityNew: 805.0m,
            ElectricityQty: 94.0m,
            ElectricityUnitPrice: 3500m,
            ElectricityAmount: 329000m,
            WaterOld: 62.0m,
            WaterNew: 67.0m,
            WaterQty: 5.0m,
            WaterUnitPrice: 30000m,
            WaterAmount: 150000m,
            AdditionalFeeTotal: 150000m,
            TotalAmount: 4429000m,
            Status: BillStatus.Paid,
            IssuedAt: new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.FromHours(7)),
            DueDate: new DateOnly(2026, 7, 10),
            PaidAt: new DateTimeOffset(2026, 7, 5, 14, 30, 0, TimeSpan.FromHours(7)),
            AdditionalFees:
            [
                new RoomAdditionalFeeResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Internet cáp quang & Wifi", 100000m, 7, 2026, DateTimeOffset.UtcNow),
                new RoomAdditionalFeeResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Vệ sinh & Rác thải", 50000m, 7, 2026, DateTimeOffset.UtcNow)
            ],
            TenantSplits:
            [
                new TenantBillSplitResponse(Guid.NewGuid(), Guid.NewGuid(), "Lê Đức Thắng", true, 2214500m),
                new TenantBillSplitResponse(Guid.NewGuid(), Guid.NewGuid(), "Trần Quốc Duy", false, 2214500m)
            ],
            CreatedAt: new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.FromHours(7))
        );

        var viPdf = generator.Generate(bill, "vi");
        var enPdf = generator.Generate(bill, "en");

        Assert.NotNull(viPdf);
        Assert.NotEmpty(viPdf);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(viPdf.Take(5).ToArray()));

        Assert.NotNull(enPdf);
        Assert.NotEmpty(enPdf);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(enPdf.Take(5).ToArray()));

        File.WriteAllBytes("/tmp/test_bill_vi.pdf", viPdf);
        File.WriteAllBytes("/tmp/test_bill_en.pdf", enPdf);
    }
}
