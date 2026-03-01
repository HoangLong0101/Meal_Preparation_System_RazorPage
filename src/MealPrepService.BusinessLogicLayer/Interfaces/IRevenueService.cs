using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Service interface for revenue reporting and analytics operations
    /// </summary>
    public interface IRevenueService
    {
        Task<RevenueReportDto> GenerateMonthlyReportAsync(int year, int month);
        Task<RevenueReportDto> GetMonthlyReportAsync(int year, int month);
        Task<decimal> GetYearlyRevenueAsync(int year);
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}