using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Recipes
{
    public class DetailsModel : PageModel
    {
        private readonly IRecipeService _recipeService;

        public DetailsModel(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        public RecipeDto Recipe { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                Recipe = await _recipeService.GetByIdAsync(id);
                return Page();
            }
            catch
            {
                return RedirectToPage("/Recipes/Index");
            }
        }
    }
}
