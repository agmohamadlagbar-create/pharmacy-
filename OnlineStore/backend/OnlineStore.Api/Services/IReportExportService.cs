using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface IReportExportService
{
    byte[] GenerateMonthlyPdf(MonthlyReportDto report, string shopName);
    byte[] GenerateYearlyPdf(YearlyReportDto report, string shopName);
    byte[] GenerateMonthlyExcel(MonthlyReportDto report, string shopName);
    byte[] GenerateYearlyExcel(YearlyReportDto report, string shopName);
}
