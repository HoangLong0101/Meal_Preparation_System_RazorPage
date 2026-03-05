using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IRevenueService _revenueService;

        public DashboardModel(IRevenueService revenueService)
        {
            _revenueService = revenueService;
        }

        public DashboardStatsDto Stats { get; set; } = new();
        public RevenueReportDto? CurrentMonthReport { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            Stats = await _revenueService.GetDashboardStatsAsync();

            try
            {
                CurrentMonthReport = await _revenueService.GetMonthlyReportAsync(DateTime.Today.Year, DateTime.Today.Month);
            }
            catch { }

            return Page();
        }
    }
}
