using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealPlans
{
    public class AddMealModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;
        private readonly IRecipeService _recipeService;

        public AddMealModel(IMealPlanService mealPlanService, IRecipeService recipeService)
        {
            _mealPlanService = mealPlanService;
            _recipeService = recipeService;
        }

        public IEnumerable<MealPlanDto> MealPlans { get; set; } = [];
        public RecipeDto? Recipe { get; set; }
        public string? SourcePage { get; set; }

        [BindProperty]
        public Guid RecipeId { get; set; }

        [BindProperty]
        public Guid SelectedPlanId { get; set; }

        [BindProperty]
        public DateTime ServeDate { get; set; } = DateTime.Today;

        [BindProperty]
        public string MealType { get; set; } = "lunch";

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid recipeId, string? returnUrl)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);
            ReturnUrl = returnUrl;

            try
            {
                Recipe = await _recipeService.GetByIdAsync(recipeId);
                RecipeId = recipeId;
            }
            catch
            {
                TempData["ErrorMessage"] = "Recipe not found.";
                return RedirectToPage("/Recipes/Index");
            }

            MealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);

            if (!MealPlans.Any())
            {
                TempData["ErrorMessage"] = "You don't have any meal plans yet. Create one first.";
                return RedirectToPage("/MealPlans/Generate");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);

            try
            {
                var mealDto = new MealDto
                {
                    MealType = MealType,
                    ServeDate = ServeDate,
                    Recipes = new List<RecipeDto> { new RecipeDto { Id = RecipeId } }
                };

                await _mealPlanService.AddMealToPlanAsync(SelectedPlanId, mealDto);
                TempData["SuccessMessage"] = "Recipe added to meal plan!";

                return RedirectToPage("/MealPlans/Details", new { id = SelectedPlanId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                MealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
                try { Recipe = await _recipeService.GetByIdAsync(RecipeId); } catch { }

                return Page();
            }
        }
    }
}
