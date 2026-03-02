using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Recipes
{
    public class CreateModel : PageModel
    {
        private readonly IRecipeService _recipeService;

        public CreateModel(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        [BindProperty]
        public CreateRecipeDto Input { get; set; } = new();

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToPage("/Account/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var recipe = await _recipeService.CreateRecipeAsync(Input);
                return RedirectToPage("/Recipes/Details", new { id = recipe.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostGenerateAIAsync(string cuisineType)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var recipes = await _recipeService.GenerateAndSaveAIRecipeAsync(
                    Guid.Parse(accountIdStr), cuisineType ?? "Vietnamese", 3);
                TempData["SuccessMessage"] = $"{recipes.Count} AI recipes generated successfully!";
                return RedirectToPage("/Recipes/Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
        }
    }
}
