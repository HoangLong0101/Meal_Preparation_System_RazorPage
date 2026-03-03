using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealPlans
{
    public class GenerateModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;

        public GenerateModel(IMealPlanService mealPlanService)
        {
            _mealPlanService = mealPlanService;
        }

        [BindProperty]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [BindProperty]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(6);

        [BindProperty]
        public string? PlanName { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToPage("/Account/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var plan = await _mealPlanService.GenerateAiMealPlanAsync(
                    Guid.Parse(accountIdStr), StartDate, EndDate, PlanName);
                TempData["SuccessMessage"] = "AI Meal Plan generated successfully!";
                return RedirectToPage("/MealPlans/Details", new { id = plan.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
        }
    }
}
