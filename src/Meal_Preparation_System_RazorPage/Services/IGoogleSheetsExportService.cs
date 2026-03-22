using MealPrepService.BusinessLogicLayer.DTOs;

namespace Meal_Preparation_System_RazorPage.Services
{
    public interface IGoogleSheetsExportService
    {
        Task ExportDashboardReportAsync(DashboardExportPayload payload);
    }

    public class DashboardExportPayload
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public RevenueReportDto? CurrentMonthReport { get; set; }
        public List<RevenueTrendPointDto> RevenueTrend { get; set; } = new();
        public List<DishRevenueDto> TopSellingDishes { get; set; } = new();
    }
}
