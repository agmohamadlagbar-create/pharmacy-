using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface IAnalyticsService
{
    Task<FullAnalyticsDto> GetFullAnalyticsAsync(string storeUserId, DateTime? startDate, DateTime? endDate);
    Task<MonthlyReportDto> GetMonthlyReportAsync(string storeUserId, int year, int month);
    Task<YearlyReportDto> GetYearlyReportAsync(string storeUserId, int year);
}
