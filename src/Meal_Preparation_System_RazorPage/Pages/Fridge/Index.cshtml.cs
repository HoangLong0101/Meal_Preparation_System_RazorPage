using System.Text.Json;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Fridge
{
    public class IndexModel : PageModel
    {
        private readonly IFridgeService _fridgeService;
        private readonly IIngredientService _ingredientService;

        public IndexModel(IFridgeService fridgeService, IIngredientService ingredientService)
        {
            _fridgeService = fridgeService;
            _ingredientService = ingredientService;
        }

        public IEnumerable<FridgeItemDto> FridgeItems { get; set; } = [];
        public IEnumerable<FridgeItemDto> ExpiringItems { get; set; } = [];
        public IEnumerable<IngredientDto> AvailableIngredients { get; set; } = [];

        [BindProperty]
        public FridgeItemDto NewItem { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);
            FridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            ExpiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
            AvailableIngredients = await _ingredientService.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                NewItem.AccountId = Guid.Parse(accountIdStr);
                await _fridgeService.AddItemAsync(NewItem);
                TempData["SuccessMessage"] = "Item added to fridge.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(Guid itemId)
        {
            try
            {
                await _fridgeService.RemoveItemAsync(itemId);
                TempData["SuccessMessage"] = "Item removed.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(Guid itemId, float newQuantity)
        {
            try
            {
                await _fridgeService.UpdateItemQuantityAsync(itemId, newQuantity);
                TempData["SuccessMessage"] = "Quantity updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmShoppingListAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var shoppingListJson = HttpContext.Session.GetString("ShoppingList");
            if (string.IsNullOrEmpty(shoppingListJson))
            {
                TempData["ErrorMessage"] = "No shopping list found.";
                return RedirectToPage();
            }

            var shoppingItems = JsonSerializer.Deserialize<List<ShoppingListItem>>(shoppingListJson);
            if (shoppingItems == null || !shoppingItems.Any())
            {
                HttpContext.Session.Remove("ShoppingList");
                return RedirectToPage();
            }

            var accountId = Guid.Parse(accountIdStr);
            var allIngredients = await _ingredientService.GetAllAsync();
            var ingredientLookup = allIngredients
                .ToDictionary(i => i.IngredientName.ToLowerInvariant(), i => i);

            int addedCount = 0;
            var skipped = new List<string>();

            foreach (var item in shoppingItems)
            {
                if (ingredientLookup.TryGetValue(item.IngredientName.ToLowerInvariant(), out var ingredient))
                {
                    try
                    {
                        await _fridgeService.AddItemAsync(new FridgeItemDto
                        {
                            AccountId = accountId,
                            IngredientId = ingredient.Id,
                            CurrentAmount = item.NeededAmount,
                            ExpiryDate = DateTime.UtcNow.AddDays(7)
                        });
                        addedCount++;
                    }
                    catch
                    {
                        skipped.Add(item.IngredientName);
                    }
                }
                else
                {
                    skipped.Add(item.IngredientName);
                }
            }

            HttpContext.Session.Remove("ShoppingList");

            if (skipped.Any())
                TempData["SuccessMessage"] = $"{addedCount} item(s) added to inventory. {skipped.Count} skipped (not found): {string.Join(", ", skipped)}.";
            else
                TempData["SuccessMessage"] = $"{addedCount} item(s) added to inventory!";

            return RedirectToPage();
        }
    }
}
