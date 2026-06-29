using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface IReportExportService
{
    byte[] GenerateMonthlyPdf(MonthlyReportDto report, string shopName, string lang);
    byte[] GenerateYearlyPdf(YearlyReportDto report, string shopName, string lang);
    byte[] GenerateMonthlyExcel(MonthlyReportDto report, string shopName, string lang);
    byte[] GenerateYearlyExcel(YearlyReportDto report, string shopName, string lang);
}
