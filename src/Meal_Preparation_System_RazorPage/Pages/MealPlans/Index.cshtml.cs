using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealPlans
{
    public class IndexModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;

        public IndexModel(IMealPlanService mealPlanService)
        {
            _mealPlanService = mealPlanService;
        }

        public IEnumerable<MealPlanDto> MealPlans { get; set; } = [];
        public MealPlanDto? ActivePlan { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);
            MealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            ActivePlan = await _mealPlanService.GetActivePlanAsync(accountId);
            return Page();
        }

        public async Task<IActionResult> OnPostSetActiveAsync(Guid planId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                await _mealPlanService.SetActivePlanAsync(planId, Guid.Parse(accountIdStr));
                TempData["SuccessMessage"] = "Active plan updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid planId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                await _mealPlanService.DeleteAsync(planId, Guid.Parse(accountIdStr));
                TempData["SuccessMessage"] = "Meal plan deleted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
