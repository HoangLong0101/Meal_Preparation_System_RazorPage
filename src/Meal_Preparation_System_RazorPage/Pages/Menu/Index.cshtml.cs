using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Menu
{
    public class IndexModel : PageModel
    {
        private readonly IMenuService _menuService;

        public IndexModel(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public DailyMenuDto? TodayMenu { get; set; }
        public IEnumerable<DailyMenuDto> WeeklyMenus { get; set; } = [];

        public async Task OnGetAsync()
        {
            TodayMenu = await _menuService.GetByDateAsync(DateTime.Today);

            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            WeeklyMenus = await _menuService.GetWeeklyMenuAsync(startOfWeek);
        }
    }
}
