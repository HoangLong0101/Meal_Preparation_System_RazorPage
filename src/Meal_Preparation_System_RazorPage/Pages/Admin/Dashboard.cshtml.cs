using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Meal_Preparation_System_RazorPage.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IRevenueService _revenueService;
        private readonly ILLMService _llmService;

        public DashboardModel(IRevenueService revenueService, ILLMService llmService)
        {
            _revenueService = revenueService;
            _llmService = llmService;
        }

        public DashboardStatsDto Stats { get; set; } = new();
        public RevenueReportDto? CurrentMonthReport { get; set; }
        public List<DishRevenueDto> TopSellingDishes { get; set; } = new();
        public List<RevenueTrendPointDto> RevenueTrend { get; set; } = new();
        public List<AIGeneratedRecipe> AiSuggestedMenus { get; set; } = new();
        public string AiInsightSummary { get; set; } = "Bấm 'Dự đoán xu hướng người dùng' để gọi AI và nhận gợi ý menu.";
        public bool HasAiPrediction { get; set; }
        public string RevenueTrendJson => JsonSerializer.Serialize(RevenueTrend.Select(item => item.Label));
        public string RevenueDataJson => JsonSerializer.Serialize(RevenueTrend.Select(item => item.Revenue));
        public string DishLabelsJson => JsonSerializer.Serialize(TopSellingDishes.Select(item => item.RecipeName));
        public string DishRevenueJson => JsonSerializer.Serialize(TopSellingDishes.Select(item => item.TotalRevenue));

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            await LoadDashboardDataAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostPredictDemandAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            await LoadDashboardDataAsync();
            await LoadAiDemandInsightsAsync();

            return Page();
        }

        private async Task LoadDashboardDataAsync()
        {
            Stats = await _revenueService.GetDashboardStatsAsync();
            RevenueTrend = await _revenueService.GetRevenueTrendAsync(6);

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            TopSellingDishes = await _revenueService.GetTopSellingDishesAsync(monthStart, nextMonthStart, 5);

            try
            {
                CurrentMonthReport = await _revenueService.GetMonthlyReportAsync(DateTime.Today.Year, DateTime.Today.Month);
            }
            catch
            {
                CurrentMonthReport = null;
            }
        }

        private async Task LoadAiDemandInsightsAsync()
        {
            if (!TopSellingDishes.Any())
            {
                AiInsightSummary = "Chưa đủ dữ liệu bán hàng để AI phân tích nhu cầu.";
                HasAiPrediction = false;
                return;
            }

            try
            {
                var trendSummary = string.Join(", ", RevenueTrend.Select(item => $"{item.Label}: {item.Revenue:N0} VND"));
                var dishSummary = string.Join(", ", TopSellingDishes.Select(item =>
                    $"{item.RecipeName} (SL: {item.TotalQuantitySold}, DT: {item.TotalRevenue:N0} VND)"));

                var aiContext = $"Top món tháng này: {dishSummary}. Xu hướng 6 tháng: {trendSummary}. " +
                                "Phân tích nhu cầu người dùng và đề xuất menu tuần tới để tối ưu doanh thu, ưu tiên món Việt dễ triển khai.";

                AiSuggestedMenus = await _llmService.GenerateRecipeSuggestionsAsync(
                    cuisineType: "Vietnamese healthy",
                    dietaryRestrictions: aiContext,
                    calorieTarget: 550,
                    count: 3);

                AiInsightSummary = "AI đã phân tích dữ liệu bán hàng và đề xuất menu ưu tiên theo nhu cầu hiện tại.";
                HasAiPrediction = true;
            }
            catch
            {
                AiInsightSummary = "Không thể tải AI insight lúc này. Vui lòng kiểm tra cấu hình AI và thử lại.";
                AiSuggestedMenus = new List<AIGeneratedRecipe>();
                HasAiPrediction = false;
            }
        }
    }
}
