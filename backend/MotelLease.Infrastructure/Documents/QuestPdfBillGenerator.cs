using System.Globalization;
using MotelLease.Application.Bills.Contracts;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Enums;
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
        var isVi = language.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var culture = isVi ? CultureInfo.GetCultureInfo("vi-VN") : CultureInfo.GetCultureInfo("en-US");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Liberation Sans"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Left: House name & Room info
                        row.RelativeItem(7.5f).Column(c =>
                        {
                            c.Item().Text(bill.BoardingHouseName).Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            c.Item().PaddingTop(2).Text(isVi ? $"Phòng: {bill.RoomNumber}" : $"Room: {bill.RoomNumber}").Bold().FontSize(11);
                            c.Item().Text(isVi ? $"Kỳ thanh toán: {bill.Month:D2}/{bill.Year}" : $"Billing Period: {bill.Month:D2}/{bill.Year}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        // Right: Title, Status badge & Dates
                        row.RelativeItem(4.5f).Column(c =>
                        {
                            c.Item().Text(isVi ? "HÓA ĐƠN TIỀN NHÀ" : "RENTAL INVOICE").Bold().FontSize(15).FontColor(Colors.Grey.Darken3).AlignRight();

                            var (statusText, statusBg, statusFg) = bill.Status switch
                            {
                                BillStatus.Paid => (isVi ? "Đã thanh toán" : "Paid", Colors.Green.Lighten4, Colors.Green.Darken3),
                                BillStatus.Issued => (isVi ? "Chờ thanh toán" : "Pending", Colors.Orange.Lighten4, Colors.Orange.Darken3),
                                BillStatus.Draft => (isVi ? "Bản nháp" : "Draft", Colors.Grey.Lighten3, Colors.Grey.Darken2),
                                BillStatus.Cancelled => (isVi ? "Đã hủy" : "Cancelled", Colors.Red.Lighten4, Colors.Red.Darken3),
                                _ => (bill.Status.ToString(), Colors.Grey.Lighten3, Colors.Grey.Darken2)
                            };

                            c.Item().PaddingTop(3).Row(badgeRow =>
                            {
                                badgeRow.RelativeItem();
                                badgeRow.AutoItem().Background(statusBg).PaddingVertical(2).PaddingHorizontal(8).Text(statusText).Bold().FontSize(9).FontColor(statusFg);
                            });

                            if (bill.DueDate.HasValue)
                            {
                                c.Item().PaddingTop(4).Text(isVi ? $"Hạn thanh toán: {bill.DueDate.Value:dd/MM/yyyy}" : $"Due Date: {bill.DueDate.Value:dd/MM/yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1).AlignRight();
                            }
                            if (bill.PaidAt.HasValue && bill.Status == BillStatus.Paid)
                            {
                                c.Item().Text(isVi ? $"Ngày thanh toán: {bill.PaidAt.Value:dd/MM/yyyy}" : $"Paid Date: {bill.PaidAt.Value:dd/MM/yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1).AlignRight();
                            }
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3.8f);
                            columns.RelativeColumn(2.6f);
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(2.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(isVi ? "Khoản mục" : "Item").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(isVi ? "Chỉ số / Số lượng" : "Qty / Readings").Bold().FontSize(10).AlignCenter();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(isVi ? "Đơn giá" : "Unit Price").Bold().FontSize(10).AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(isVi ? "Thành tiền" : "Amount").Bold().FontSize(10).AlignRight();
                        });

                        void AddRow(string item, string qty, decimal unitPrice, decimal amount)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(item);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(qty).AlignCenter();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(unitPrice.ToString("N0", culture)).AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).PaddingHorizontal(8).Text(amount.ToString("N0", culture)).AlignRight();
                        }

                        // 1. Rent
                        AddRow(
                            isVi ? "Tiền thuê phòng" : "Monthly Rent",
                            isVi ? "1 tháng" : "1 month",
                            bill.RentAmount,
                            bill.RentAmount
                        );

                        // 2. Electricity
                        AddRow(
                            isVi ? "Tiền điện" : "Electricity",
                            $"{bill.ElectricityOld:N1} → {bill.ElectricityNew:N1} ({bill.ElectricityQty:N1} kWh)",
                            bill.ElectricityUnitPrice,
                            bill.ElectricityAmount
                        );

                        // 3. Water
                        AddRow(
                            isVi ? "Tiền nước" : "Water",
                            $"{bill.WaterOld:N1} → {bill.WaterNew:N1} ({bill.WaterQty:N1} m³)",
                            bill.WaterUnitPrice,
                            bill.WaterAmount
                        );

                        // 4. Additional fees
                        foreach (var fee in bill.AdditionalFees)
                        {
                            AddRow(
                                fee.FeeName,
                                "1",
                                fee.FeeAmount,
                                fee.FeeAmount
                            );
                        }
                    });

                    // Total Card
                    col.Item().PaddingTop(12).Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(10).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle().Text(isVi ? "TỔNG TIỀN THANH TOÁN:" : "TOTAL AMOUNT DUE:").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                        row.AutoItem().AlignMiddle().Text($"{bill.TotalAmount.ToString("N0", culture)} {(isVi ? "VNĐ" : "VND")}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                    });

                    // Tenant splits
                    if (bill.TenantSplits.Count > 0)
                    {
                        col.Item().PaddingTop(18).Text(isVi ? "Chi tiết phân bổ theo người thuê:" : "Per-Tenant Breakdown:").Bold().FontSize(11).FontColor(Colors.Grey.Darken3);

                        col.Item().Table(splitTable =>
                        {
                            splitTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3.8f);
                                columns.RelativeColumn(2.6f);
                                columns.RelativeColumn(2.6f);
                            });

                            splitTable.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(8).Text(isVi ? "Người thuê" : "Tenant").Bold().FontSize(9);
                                h.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(8).Text(isVi ? "Vai trò" : "Role").Bold().FontSize(9).AlignCenter();
                                h.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(8).Text(isVi ? "Số tiền" : "Share").Bold().FontSize(9).AlignRight();
                            });

                            foreach (var split in bill.TenantSplits)
                            {
                                splitTable.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(8).Text(split.FullName).FontSize(9);
                                splitTable.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(8).Text(split.IsPrimary ? (isVi ? "Đại diện hợp đồng" : "Primary Tenant") : (isVi ? "Thành viên ở cùng" : "Co-tenant")).FontSize(9).AlignCenter();
                                splitTable.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(8).Text(split.Amount.ToString("N0", culture)).Bold().FontSize(9).AlignRight();
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

        return document.GeneratePdf();
    }
}
