using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Fridge
{
    public class SmartMealsModel : PageModel
    {
        private readonly IFridgeService _fridgeService;
        private readonly ILLMService _llmService;
        private readonly ICustomerProfileAnalyzer _profileAnalyzer;
        private readonly IRecipeService _recipeService;
        private readonly IFamilyMemberService _familyMemberService;
        private readonly IMealPlanService _mealPlanService;

        public SmartMealsModel(
            IFridgeService fridgeService,
            ILLMService llmService,
            ICustomerProfileAnalyzer profileAnalyzer,
            IRecipeService recipeService,
            IFamilyMemberService familyMemberService,
            IMealPlanService mealPlanService)
        {
            _fridgeService = fridgeService;
            _llmService = llmService;
            _profileAnalyzer = profileAnalyzer;
            _recipeService = recipeService;
            _familyMemberService = familyMemberService;
            _mealPlanService = mealPlanService;
        }

        public IEnumerable<FridgeItemDto> FridgeItems { get; set; } = [];
        public IEnumerable<FridgeItemDto> ExpiringItems { get; set; } = [];

        [BindProperty]
        public string MealType { get; set; } = "Lunch";

        [BindProperty]
        public string? MealPreference { get; set; }

        [BindProperty]
        public int? MaxPrepTime { get; set; }

        [BindProperty]
        public bool PrioritizeExpiring { get; set; } = true;

        [BindProperty]
        public int Servings { get; set; } = 1;

        public InventoryMealSuggestionResponse? SuggestionResult { get; set; }

        public bool HasGenerated { get; set; }

        public IEnumerable<FamilyMemberDto> FamilyMembers { get; set; } = [];
        public int MaxFamilySize { get; set; } = 1;

        public IEnumerable<MealPlanDto> UserMealPlans { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);
            await LoadInventoryAsync(accountId);
            await LoadFamilyAsync(accountId);
            UserMealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);
            await LoadInventoryAsync(accountId);
            await LoadFamilyAsync(accountId);
            UserMealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);

            if (!FridgeItems.Any())
            {
                TempData["ErrorMessage"] = "Your inventory is empty. Add some ingredients first.";
                return Page();
            }

            try
            {
                var customerContext = await _profileAnalyzer.AnalyzeCustomerAsync(accountId);

                var inventoryIngredients = FridgeItems.Select(fi => new InventoryIngredient
                {
                    IngredientId = fi.IngredientId,
                    Name = fi.IngredientName,
                    AvailableAmount = fi.CurrentAmount,
                    Unit = fi.Unit,
                    ExpiryDate = fi.ExpiryDate
                }).ToList();

                var request = new InventoryMealRequest
                {
                    AvailableIngredients = inventoryIngredients,
                    MealType = MealType,
                    MaxPrepTime = MaxPrepTime,
                    PrioritizeExpiring = PrioritizeExpiring,
                    GenerateShoppingList = true,
                    MealPreference = MealPreference
                };

                var recipeEntities = new List<MealPrepService.DataAccessLayer.Entities.Recipe>();

                SuggestionResult = await _llmService.GenerateInventoryBasedMealAsync(
                    customerContext, request, recipeEntities);

                HasGenerated = true;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"AI generation failed: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddToPlanAsync(Guid recipeId, Guid planId, DateTime serveDate, string mealType, string? missingIngredientsJson)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var mealDto = new MealDto
                {
                    MealType = mealType,
                    ServeDate = serveDate,
                    Recipes = new List<RecipeDto> { new RecipeDto { Id = recipeId } }
                };

                await _mealPlanService.AddMealToPlanAsync(planId, mealDto);
                TempData["SuccessMessage"] = "Recipe added to meal plan!";
                StoreMissingIngredients(missingIngredientsJson);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveAndAddToPlanAsync(
            string recipeName, string? instructions, float calories, float protein, float carbs, float fat,
            Guid planId, DateTime serveDate, string mealType, string? missingIngredientsJson)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var createDto = new CreateRecipeDto
                {
                    RecipeName = recipeName,
                    Instructions = instructions ?? string.Empty,
                    TotalCalories = calories,
                    ProteinG = protein,
                    FatG = fat,
                    CarbsG = carbs
                };

                var savedRecipe = await _recipeService.CreateRecipeAsync(createDto);

                var mealDto = new MealDto
                {
                    MealType = mealType,
                    ServeDate = serveDate,
                    Recipes = new List<RecipeDto> { new RecipeDto { Id = savedRecipe.Id } }
                };

                await _mealPlanService.AddMealToPlanAsync(planId, mealDto);
                TempData["SuccessMessage"] = $"'{recipeName}' saved and added to meal plan!";
                StoreMissingIngredients(missingIngredientsJson);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        private void StoreMissingIngredients(string? missingIngredientsJson)
        {
            if (!string.IsNullOrEmpty(missingIngredientsJson) && missingIngredientsJson != "[]")
            {
                HttpContext.Session.SetString("ShoppingList", missingIngredientsJson);
            }
        }

        private async Task LoadInventoryAsync(Guid accountId)
        {
            FridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            ExpiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
        }

        private async Task LoadFamilyAsync(Guid accountId)
        {
            FamilyMembers = await _familyMemberService.GetByAccountIdAsync(accountId);
            MaxFamilySize = FamilyMembers.Count() + 1;
            if (MaxFamilySize < 1) MaxFamilySize = 1;
            if (Servings < 1) Servings = MaxFamilySize;
        }
    }
}
