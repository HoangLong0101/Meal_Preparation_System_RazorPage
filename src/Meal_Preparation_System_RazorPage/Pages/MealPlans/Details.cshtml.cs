using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealPlans
{
    public class DetailsModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;
        private readonly IRecipeService _recipeService;

        public DetailsModel(IMealPlanService mealPlanService, IRecipeService recipeService)
        {
            _mealPlanService = mealPlanService;
            _recipeService = recipeService;
        }

        public MealPlanDto Plan { get; set; } = new();
        public IEnumerable<RecipeDto> AllRecipes { get; set; } = [];

        [BindProperty]
        public Guid AddRecipeId { get; set; }

        [BindProperty]
        public DateTime AddServeDate { get; set; } = DateTime.Today;

        [BindProperty]
        public string AddMealType { get; set; } = "lunch";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var plan = await _mealPlanService.GetByIdAsync(id);
            if (plan == null)
                return RedirectToPage("/MealPlans/Index");

            Plan = plan;
            AllRecipes = await _recipeService.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleMealFinishedAsync(Guid id, Guid mealId, bool finished)
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

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostToggleRecipeFinishedAsync(Guid id, Guid mealId, Guid recipeId, bool finished)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                await _mealPlanService.MarkRecipeInMealAsFinishedAsync(mealId, recipeId, Guid.Parse(accountIdStr), finished);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddRecipeAsync(Guid id)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var mealDto = new MealDto
                {
                    MealType = AddMealType,
                    ServeDate = AddServeDate,
                    Recipes = new List<RecipeDto> { new RecipeDto { Id = AddRecipeId } }
                };

                await _mealPlanService.AddMealToPlanAsync(id, mealDto);
                TempData["SuccessMessage"] = "Recipe added to plan!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveRecipeAsync(Guid id, Guid mealId, Guid recipeId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                await _mealPlanService.RemoveRecipeFromMealAsync(mealId, recipeId, Guid.Parse(accountIdStr));
                TempData["SuccessMessage"] = "Recipe removed from meal.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { id });
        }
    }
}
