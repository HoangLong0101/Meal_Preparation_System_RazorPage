using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Recipes
{
    public class DetailsModel : PageModel
    {
        private readonly IRecipeService _recipeService;
        private readonly IFridgeService _fridgeService;

        public DetailsModel(IRecipeService recipeService, IFridgeService fridgeService)
        {
            _recipeService = recipeService;
            _fridgeService = fridgeService;
        }

        public RecipeDto Recipe { get; set; } = new();
        public List<ShoppingCompareItem> ShoppingList { get; set; } = [];
        public bool IsLoggedIn { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                Recipe = await _recipeService.GetByIdAsync(id);

                var accountIdStr = HttpContext.Session.GetString("AccountId");
                if (accountIdStr != null)
                {
                    IsLoggedIn = true;
                    var accountId = Guid.Parse(accountIdStr);
                    var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);

                    foreach (var ing in Recipe.Ingredients)
                    {
                        var inFridge = fridgeItems
                            .FirstOrDefault(fi => fi.IngredientId == ing.IngredientId);

                        ShoppingList.Add(new ShoppingCompareItem
                        {
                            IngredientName = ing.IngredientName,
                            RequiredAmount = ing.Amount,
                            Unit = ing.Unit,
                            AvailableAmount = inFridge?.CurrentAmount ?? 0,
                            IsAvailable = inFridge != null && inFridge.CurrentAmount >= ing.Amount,
                            IsPartial = inFridge != null && inFridge.CurrentAmount > 0 && inFridge.CurrentAmount < ing.Amount,
                            MissingAmount = inFridge != null
                                ? Math.Max(0, ing.Amount - inFridge.CurrentAmount)
                                : ing.Amount
                        });
                    }
                }

                return Page();
            }
            catch
            {
                return RedirectToPage("/Recipes/Index");
            }
        }
    }

    public class ShoppingCompareItem
    {
        public string IngredientName { get; set; } = string.Empty;
        public float RequiredAmount { get; set; }
        public string Unit { get; set; } = string.Empty;
        public float AvailableAmount { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsPartial { get; set; }
        public float MissingAmount { get; set; }
    }
}
