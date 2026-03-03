using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Recipes
{
    public class IndexModel : PageModel
    {
        private readonly IRecipeService _recipeService;

        public IndexModel(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        public IEnumerable<RecipeDto> Recipes { get; set; } = [];

        public async Task OnGetAsync()
        {
            Recipes = await _recipeService.GetAllWithIngredientsAsync();
        }
    }
}
