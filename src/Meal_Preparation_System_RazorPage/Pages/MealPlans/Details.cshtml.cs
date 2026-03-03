using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealPlans
{
    public class DetailsModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;

        public DetailsModel(IMealPlanService mealPlanService)
        {
            _mealPlanService = mealPlanService;
        }

        public MealPlanDto Plan { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var plan = await _mealPlanService.GetByIdAsync(id);
            if (plan == null)
                return RedirectToPage("/MealPlans/Index");

            Plan = plan;
            return Page();
        }

        public async Task<IActionResult> OnPostToggleMealFinishedAsync(Guid mealId, bool finished)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                await _mealPlanService.MarkMealAsFinishedAsync(mealId, Guid.Parse(accountIdStr), finished);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
