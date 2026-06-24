using ClosedXML.Excel;
using OnlineStore.Api.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OnlineStore.Api.Services;

public class ReportExportService : IReportExportService
{
    // ═══════════════════════════════════════════════════
    //  PDF — Monthly Report
    // ═══════════════════════════════════════════════════

    public byte[] GenerateMonthlyPdf(MonthlyReportDto report, string shopName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, shopName, $"Monthly Sales Report — {report.MonthName} {report.Year}"));

                page.Content().Element(content =>
                {
                    content.PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        // Summary section
                        col.Item().Element(e => ComposeSummarySection(e,
                            report.CurrencySymbol, report.TotalRevenue, report.TotalOrders,
                            report.AverageOrderValue, report.RevenueGrowthPercent, report.OrderGrowthPercent));

                        // Top Products Table
                        if (report.TopProducts.Count > 0)
                        {
                            col.Item().Element(e => ComposeTopProductsTable(e, report.TopProducts, report.CurrencySymbol));
                        }

                        // Daily Sales Table
                        if (report.DailySales.Count > 0)
                        {
                            col.Item().Element(e => ComposeDailySalesTable(e, report.DailySales, report.CurrencySymbol));
                        }
                    });
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════
    //  PDF — Yearly Report
    // ═══════════════════════════════════════════════════

    public byte[] GenerateYearlyPdf(YearlyReportDto report, string shopName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, shopName, $"Yearly Sales Report — {report.Year}"));

                page.Content().Element(content =>
                {
                    content.PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        // Summary
                        col.Item().Text(text =>
                        {
                            text.Span("Total Revenue: ").Bold();
                            text.Span($"{report.CurrencySymbol}{report.TotalRevenue:N2}");
                        });
                        col.Item().Text(text =>
                        {
                            text.Span("Total Orders: ").Bold();
                            text.Span($"{report.TotalOrders:N0}");
                        });
                        col.Item().Text(text =>
                        {
                            text.Span("Average Order Value: ").Bold();
                            text.Span($"{report.CurrencySymbol}{report.AverageOrderValue:N2}");
                        });

                        // Monthly Breakdown
                        if (report.MonthlyBreakdown.Count > 0)
                        {
                            col.Item().PaddingTop(10).Text("Monthly Revenue Breakdown").Bold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Month").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Revenue").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Orders").Bold();
                                });

                                foreach (var m in report.MonthlyBreakdown)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(m.Month);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{report.CurrencySymbol}{m.Revenue:N2}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{m.OrderCount}");
                                }
                            });
                        }

                        // Growth Trends
                        if (report.GrowthTrends.Count > 0)
                        {
                            col.Item().PaddingTop(10).Text("Growth Trends (Month-over-Month)").Bold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Month").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Revenue Growth").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Order Growth").Bold();
                                });

                                foreach (var g in report.GrowthTrends)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(g.Month);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{g.RevenueGrowthPercent:+0.0;-0.0;0.0}%");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{g.OrderGrowthPercent:+0.0;-0.0;0.0}%");
                                }
                            });
                        }

                        // Top Products
                        if (report.TopProducts.Count > 0)
                        {
                            col.Item().Element(e => ComposeTopProductsTable(e, report.TopProducts, report.CurrencySymbol));
                        }
                    });
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    // ═══════════════════════════════════════════════════
    //  Excel — Monthly Report
    // ═══════════════════════════════════════════════════

    public byte[] GenerateMonthlyExcel(MonthlyReportDto report, string shopName)
    {
        using var workbook = new XLWorkbook();

        // Revenue Sheet
        var revenueSheet = workbook.Worksheets.Add("Revenue Summary");
        revenueSheet.Cell(1, 1).Value = $"{shopName} — Monthly Report: {report.MonthName} {report.Year}";
        revenueSheet.Cell(1, 1).Style.Font.Bold = true;
        revenueSheet.Cell(1, 1).Style.Font.FontSize = 14;
        revenueSheet.Range(1, 1, 1, 4).Merge();

        revenueSheet.Cell(3, 1).Value = "Metric";
        revenueSheet.Cell(3, 2).Value = "Value";
        StyleHeaderRow(revenueSheet, 3, 2);

        revenueSheet.Cell(4, 1).Value = "Total Revenue";
        revenueSheet.Cell(4, 2).Value = report.TotalRevenue;
        revenueSheet.Cell(4, 2).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";

        revenueSheet.Cell(5, 1).Value = "Total Orders";
        revenueSheet.Cell(5, 2).Value = report.TotalOrders;

        revenueSheet.Cell(6, 1).Value = "Average Order Value";
        revenueSheet.Cell(6, 2).Value = report.AverageOrderValue;
        revenueSheet.Cell(6, 2).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";

        revenueSheet.Cell(7, 1).Value = "Revenue Growth";
        revenueSheet.Cell(7, 2).Value = report.RevenueGrowthPercent / 100;
        revenueSheet.Cell(7, 2).Style.NumberFormat.Format = "0.0%";

        revenueSheet.Cell(8, 1).Value = "Order Growth";
        revenueSheet.Cell(8, 2).Value = report.OrderGrowthPercent / 100;
        revenueSheet.Cell(8, 2).Style.NumberFormat.Format = "0.0%";

        revenueSheet.Columns().AdjustToContents();

        // Daily Sales Sheet
        var dailySheet = workbook.Worksheets.Add("Daily Sales");
        dailySheet.Cell(1, 1).Value = "Date";
        dailySheet.Cell(1, 2).Value = "Revenue";
        dailySheet.Cell(1, 3).Value = "Orders";
        StyleHeaderRow(dailySheet, 1, 3);

        for (int i = 0; i < report.DailySales.Count; i++)
        {
            var ds = report.DailySales[i];
            dailySheet.Cell(i + 2, 1).Value = ds.Date;
            dailySheet.Cell(i + 2, 2).Value = ds.Revenue;
            dailySheet.Cell(i + 2, 2).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";
            dailySheet.Cell(i + 2, 3).Value = ds.OrderCount;
        }
        dailySheet.Columns().AdjustToContents();

        // Top Products Sheet
        var productsSheet = workbook.Worksheets.Add("Top Products");
        productsSheet.Cell(1, 1).Value = "Rank";
        productsSheet.Cell(1, 2).Value = "Product Name";
        productsSheet.Cell(1, 3).Value = "Quantity Sold";
        productsSheet.Cell(1, 4).Value = "Revenue";
        StyleHeaderRow(productsSheet, 1, 4);

        for (int i = 0; i < report.TopProducts.Count; i++)
        {
            var tp = report.TopProducts[i];
            productsSheet.Cell(i + 2, 1).Value = i + 1;
            productsSheet.Cell(i + 2, 2).Value = tp.ProductName;
            productsSheet.Cell(i + 2, 3).Value = tp.QuantitySold;
            productsSheet.Cell(i + 2, 4).Value = tp.Revenue;
            productsSheet.Cell(i + 2, 4).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";
        }
        productsSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════
    //  Excel — Yearly Report
    // ═══════════════════════════════════════════════════

    public byte[] GenerateYearlyExcel(YearlyReportDto report, string shopName)
    {
        using var workbook = new XLWorkbook();

        // Revenue Sheet
        var revenueSheet = workbook.Worksheets.Add("Revenue Summary");
        revenueSheet.Cell(1, 1).Value = $"{shopName} — Yearly Report: {report.Year}";
        revenueSheet.Cell(1, 1).Style.Font.Bold = true;
        revenueSheet.Cell(1, 1).Style.Font.FontSize = 14;
        revenueSheet.Range(1, 1, 1, 4).Merge();

        revenueSheet.Cell(3, 1).Value = "Metric";
        revenueSheet.Cell(3, 2).Value = "Value";
        StyleHeaderRow(revenueSheet, 3, 2);

        revenueSheet.Cell(4, 1).Value = "Total Revenue";
        revenueSheet.Cell(4, 2).Value = report.TotalRevenue;
        revenueSheet.Cell(4, 2).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";

        revenueSheet.Cell(5, 1).Value = "Total Orders";
        revenueSheet.Cell(5, 2).Value = report.TotalOrders;

        revenueSheet.Cell(6, 1).Value = "Average Order Value";
        revenueSheet.Cell(6, 2).Value = report.AverageOrderValue;
        revenueSheet.Cell(6, 2).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";

        revenueSheet.Columns().AdjustToContents();

        // Monthly Breakdown Sheet
        var monthlySheet = workbook.Worksheets.Add("Monthly Breakdown");
        monthlySheet.Cell(1, 1).Value = "Month";
        monthlySheet.Cell(1, 2).Value = "Revenue";
        monthlySheet.Cell(1, 3).Value = "Orders";
        StyleHeaderRow(monthlySheet, 1, 3);

        for (int i = 0; i < report.MonthlyBreakdown.Count; i++)
        {
            var m = report.MonthlyBreakdown[i];
            monthlySheet.Cell(i + 2, 1).Value = m.Month;
            monthlySheet.Cell(i + 2, 2).Value = m.Revenue;
            monthlySheet.Cell(i + 2, 2).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";
            monthlySheet.Cell(i + 2, 3).Value = m.OrderCount;
        }
        monthlySheet.Columns().AdjustToContents();

        // Growth Trends Sheet
        var growthSheet = workbook.Worksheets.Add("Growth Trends");
        growthSheet.Cell(1, 1).Value = "Month";
        growthSheet.Cell(1, 2).Value = "Revenue Growth %";
        growthSheet.Cell(1, 3).Value = "Order Growth %";
        StyleHeaderRow(growthSheet, 1, 3);

        for (int i = 0; i < report.GrowthTrends.Count; i++)
        {
            var g = report.GrowthTrends[i];
            growthSheet.Cell(i + 2, 1).Value = g.Month;
            growthSheet.Cell(i + 2, 2).Value = g.RevenueGrowthPercent / 100;
            growthSheet.Cell(i + 2, 2).Style.NumberFormat.Format = "0.0%";
            growthSheet.Cell(i + 2, 3).Value = g.OrderGrowthPercent / 100;
            growthSheet.Cell(i + 2, 3).Style.NumberFormat.Format = "0.0%";
        }
        growthSheet.Columns().AdjustToContents();

        // Top Products Sheet
        var productsSheet = workbook.Worksheets.Add("Top Products");
        productsSheet.Cell(1, 1).Value = "Rank";
        productsSheet.Cell(1, 2).Value = "Product Name";
        productsSheet.Cell(1, 3).Value = "Quantity Sold";
        productsSheet.Cell(1, 4).Value = "Revenue";
        StyleHeaderRow(productsSheet, 1, 4);

        for (int i = 0; i < report.TopProducts.Count; i++)
        {
            var tp = report.TopProducts[i];
            productsSheet.Cell(i + 2, 1).Value = i + 1;
            productsSheet.Cell(i + 2, 2).Value = tp.ProductName;
            productsSheet.Cell(i + 2, 3).Value = tp.QuantitySold;
            productsSheet.Cell(i + 2, 4).Value = tp.Revenue;
            productsSheet.Cell(i + 2, 4).Style.NumberFormat.Format = $"\"{report.CurrencySymbol}\"#,##0.00";
        }
        productsSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════
    //  PDF Shared Components
    // ═══════════════════════════════════════════════════

    private static void ComposeHeader(IContainer container, string shopName, string title)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(innerCol =>
                {
                    innerCol.Item().Text(shopName).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    innerCol.Item().Text(title).FontSize(12).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Generated: ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
                });
            });
            col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeSummarySection(IContainer container, string currency,
        decimal totalRevenue, int totalOrders, decimal avgOrder,
        decimal revenueGrowth, decimal orderGrowth)
    {
        container.Column(col =>
        {
            col.Item().Text("Summary").Bold().FontSize(14);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                void AddMetric(string label, string value)
                {
                    table.Cell().Padding(5).Column(c =>
                    {
                        c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(value).FontSize(12).Bold();
                    });
                }

                AddMetric("Total Revenue", $"{currency}{totalRevenue:N2}");
                AddMetric("Total Orders", $"{totalOrders:N0}");
                AddMetric("Avg Order Value", $"{currency}{avgOrder:N2}");

                var revGrowthPrefix = revenueGrowth >= 0 ? "+" : "";
                var ordGrowthPrefix = orderGrowth >= 0 ? "+" : "";
                AddMetric("Revenue Growth", $"{revGrowthPrefix}{revenueGrowth:F1}%");
                AddMetric("Order Growth", $"{ordGrowthPrefix}{orderGrowth:F1}%");
            });
        });
    }

    private static void ComposeTopProductsTable(IContainer container, List<TopSellingProductDto> products, string currency)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(10).Text("Top Selling Products").Bold().FontSize(12);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty Sold").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Revenue").Bold();
                });

                for (int i = 0; i < products.Count; i++)
                {
                    var p = products[i];
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{i + 1}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.ProductName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{p.QuantitySold}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{currency}{p.Revenue:N2}");
                }
            });
        });
    }

    private static void ComposeDailySalesTable(IContainer container, List<DailySalesPointDto> sales, string currency)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(10).Text("Daily Sales").Bold().FontSize(12);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Date").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Revenue").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Orders").Bold();
                });

                foreach (var s in sales)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.Date);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{currency}{s.Revenue:N2}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{s.OrderCount}");
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
            text.Span(" — Generated by LAGBAR Store Analytics");
        });
    }

    // ═══════════════════════════════════════════════════
    //  Excel Helpers
    // ═══════════════════════════════════════════════════

    private static void StyleHeaderRow(IXLWorksheet sheet, int row, int colCount)
    {
        for (int c = 1; c <= colCount; c++)
        {
            var cell = sheet.Cell(row, c);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }
}
