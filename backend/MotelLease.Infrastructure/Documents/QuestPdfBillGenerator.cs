using System.Globalization;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MotelLease.Infrastructure.Documents;

public sealed class QuestPdfBillGenerator : IBillPdfGenerator
{
    static QuestPdfBillGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(BillResponse bill, string language)
    {
        var culture = language.StartsWith("vi", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("vi-VN")
            : CultureInfo.GetCultureInfo("en-US");

        var isVi = language.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(bill.BoardingHouseName).Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                            c.Item().Text($"{(isVi ? "Phòng" : "Room")} {bill.RoomNumber}").FontSize(14).SemiBold();
                            c.Item().Text($"{(isVi ? "Kỳ thanh toán" : "Billing Period")}: {bill.Month:D2}/{bill.Year}").FontSize(12);
                        });

                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text(isVi ? "HÓA ĐƠN TIỀN NHÀ" : "RENTAL INVOICE").Bold().FontSize(16).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Status: {bill.Status}").Bold().FontColor(bill.Status.ToString() == "Paid" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            if (bill.DueDate.HasValue)
                            {
                                c.Item().Text($"{(isVi ? "Hạn thanh toán" : "Due Date")}: {bill.DueDate.Value:yyyy-MM-dd}").FontSize(10);
                            }
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(isVi ? "Khoản mục" : "Item").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(isVi ? "Chỉ số / SL" : "Qty / Readings").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(isVi ? "Đơn giá" : "Unit Price").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(isVi ? "Thành tiền (VND)" : "Amount (VND)").Bold();
                        });

                        // Rent
                        table.Cell().Padding(5).Text(isVi ? "Tiền thuê phòng" : "Monthly Rent");
                        table.Cell().Padding(5).AlignRight().Text("1");
                        table.Cell().Padding(5).AlignRight().Text(bill.RentAmount.ToString("N0", culture));
                        table.Cell().Padding(5).AlignRight().Text(bill.RentAmount.ToString("N0", culture));

                        // Electricity
                        table.Cell().Padding(5).Text(isVi ? "Tiền điện" : "Electricity");
                        table.Cell().Padding(5).AlignRight().Text($"{bill.ElectricityOld:N1} → {bill.ElectricityNew:N1} ({bill.ElectricityQty:N1} kWh)");
                        table.Cell().Padding(5).AlignRight().Text(bill.ElectricityUnitPrice.ToString("N0", culture));
                        table.Cell().Padding(5).AlignRight().Text(bill.ElectricityAmount.ToString("N0", culture));

                        // Water
                        table.Cell().Padding(5).Text(isVi ? "Tiền nước" : "Water");
                        table.Cell().Padding(5).AlignRight().Text($"{bill.WaterOld:N1} → {bill.WaterNew:N1} ({bill.WaterQty:N1} m³)");
                        table.Cell().Padding(5).AlignRight().Text(bill.WaterUnitPrice.ToString("N0", culture));
                        table.Cell().Padding(5).AlignRight().Text(bill.WaterAmount.ToString("N0", culture));

                        // Additional fees
                        foreach (var fee in bill.AdditionalFees)
                        {
                            table.Cell().Padding(5).Text(fee.FeeName);
                            table.Cell().Padding(5).AlignRight().Text("1");
                            table.Cell().Padding(5).AlignRight().Text(fee.FeeAmount.ToString("N0", culture));
                            table.Cell().Padding(5).AlignRight().Text(fee.FeeAmount.ToString("N0", culture));
                        }
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // Total
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text(isVi ? "TỔNG CỘNG" : "TOTAL AMOUNT").Bold().FontSize(14);
                        row.RelativeItem().AlignRight().Text($"{bill.TotalAmount.ToString("N0", culture)} VND").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                    });

                    // Tenant splits
                    if (bill.TenantSplits.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text(isVi ? "Chia tiền theo người thuê:" : "Per-Tenant Split:").Bold().FontSize(12);

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(isVi ? "Người thuê" : "Tenant").Bold();
                                header.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(isVi ? "Vai trò" : "Role").Bold();
                                header.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(isVi ? "Số tiền phải trả" : "Share (VND)").Bold();
                            });

                            foreach (var split in bill.TenantSplits)
                            {
                                table.Cell().Padding(4).Text(split.FullName);
                                table.Cell().Padding(4).Text(split.IsPrimary ? (isVi ? "Đại diện" : "Primary") : (isVi ? "Ở cùng" : "Co-tenant"));
                                table.Cell().Padding(4).AlignRight().Text(split.Amount.ToString("N0", culture));
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("MotelLease Platform — ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
