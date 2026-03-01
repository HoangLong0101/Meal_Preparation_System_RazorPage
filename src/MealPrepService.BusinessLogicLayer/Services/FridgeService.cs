using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class FridgeService : IFridgeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FridgeService> _logger;

        public FridgeService(IUnitOfWork unitOfWork, ILogger<FridgeService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<FridgeItemDto>> GetFridgeItemsAsync(Guid accountId)
        {
            var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(accountId);
            return fridgeItems.Select(MapToDto);
        }

        public async Task<FridgeItemDto> AddItemAsync(FridgeItemDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            // Validate required fields
            if (dto.AccountId == Guid.Empty)
            {
                throw new BusinessException("Account ID is required");
            }

            if (dto.IngredientId == Guid.Empty)
            {
                throw new BusinessException("Ingredient ID is required");
            }

            if (dto.CurrentAmount < 0)
            {
                throw new BusinessException("Current amount cannot be negative");
            }

            if (dto.ExpiryDate <= DateTime.UtcNow.Date)
            {
                throw new BusinessException("Expiry date must be in the future");
            }

            // Verify ingredient exists
            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(dto.IngredientId);
            if (ingredient == null)
            {
                throw new BusinessException($"Ingredient with ID {dto.IngredientId} not found");
            }

            // Create fridge item entity
            var fridgeItem = new FridgeItem
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                IngredientId = dto.IngredientId,
                CurrentAmount = dto.CurrentAmount,
                ExpiryDate = dto.ExpiryDate,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FridgeItems.AddAsync(fridgeItem);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Fridge item added for account {AccountId}, ingredient {IngredientId}", 
                dto.AccountId, dto.IngredientId);

            // Load the ingredient for mapping
            fridgeItem.Ingredient = ingredient;
            return MapToDto(fridgeItem);
        }

        public async Task UpdateItemQuantityAsync(Guid itemId, float newQuantity)
        {
            if (newQuantity < 0)
            {
                throw new BusinessException("Quantity cannot be negative");
            }

            var fridgeItem = await _unitOfWork.FridgeItems.GetByIdAsync(itemId);
            if (fridgeItem == null)
            {
                throw new BusinessException($"Inventory item with ID {itemId} not found");
            }

            fridgeItem.CurrentAmount = newQuantity;
            fridgeItem.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.FridgeItems.UpdateAsync(fridgeItem);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Inventory item {ItemId} quantity updated to {NewQuantity}", 
                itemId, newQuantity);
        }

        public async Task RemoveItemAsync(Guid itemId)
        {
            var fridgeItem = await _unitOfWork.FridgeItems.GetByIdAsync(itemId);
            if (fridgeItem == null)
            {
                throw new BusinessException($"Inventory item with ID {itemId} not found");
            }

            await _unitOfWork.FridgeItems.DeleteAsync(itemId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Inventory item {ItemId} removed", itemId);
        }

        public async Task<IEnumerable<FridgeItemDto>> GetExpiringItemsAsync(Guid accountId)
        {
            var expiringItems = await _unitOfWork.FridgeItems.GetExpiringItemsAsync(accountId, 3);
            return expiringItems.Select(MapToDto);
        }

        public async Task<GroceryListDto> GenerateGroceryListAsync(Guid accountId, Guid planId)
        {
            // Get the meal plan with meals and recipes
            var mealPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(planId);
            if (mealPlan == null)
            {
                throw new BusinessException($"Meal plan with ID {planId} not found");
            }

            if (mealPlan.AccountId != accountId)
            {
                throw new BusinessException("Meal plan does not belong to the specified account");
            }

            return await GenerateGroceryListFromMealPlanAsync(accountId, mealPlan);
        }

        public async Task<GroceryListDto> GenerateGroceryListFromActivePlanAsync(Guid accountId)
        {
            // Get the active meal plan
            var allPlans = await _unitOfWork.MealPlans.GetByAccountIdAsync(accountId);
            var activePlan = allPlans.FirstOrDefault(p => p.IsActive);

            if (activePlan == null)
            {
                throw new BusinessException("No active meal plan found. Please set a meal plan as active first.");
            }

            // Get full plan with meals and recipes
            var fullPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(activePlan.Id);
            if (fullPlan == null)
            {
                throw new BusinessException($"Could not load active meal plan details");
            }

            return await GenerateGroceryListFromMealPlanAsync(accountId, fullPlan);
        }

        private async Task<GroceryListDto> GenerateGroceryListFromMealPlanAsync(Guid accountId, MealPlan mealPlan)
        {
            // Get current fridge items
            var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(accountId);
            var fridgeInventory = fridgeItems.ToDictionary(f => f.IngredientId, f => f.CurrentAmount);

            // Calculate required ingredients from meal plan and track when they're needed
            var requiredIngredients = new Dictionary<Guid, float>();
            var ingredientFirstNeededDate = new Dictionary<Guid, DateTime>();
            
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            foreach (var meal in mealPlan.Meals)
            {
                foreach (var mealRecipe in meal.MealRecipes)
                {
                    foreach (var recipeIngredient in mealRecipe.Recipe.RecipeIngredients)
                    {
                        var ingredientId = recipeIngredient.IngredientId;
                        var amount = recipeIngredient.Amount;
                        var mealDate = meal.ServeDate.Date;

                        // Track total required amount
                        if (requiredIngredients.ContainsKey(ingredientId))
                        {
                            requiredIngredients[ingredientId] += amount;
                        }
                        else
                        {
                            requiredIngredients[ingredientId] = amount;
                        }

                        // Track earliest needed date
                        if (!ingredientFirstNeededDate.ContainsKey(ingredientId) || 
                            mealDate < ingredientFirstNeededDate[ingredientId])
                        {
                            ingredientFirstNeededDate[ingredientId] = mealDate;
                        }
                    }
                }
            }

            // Generate grocery list for missing or insufficient ingredients
            var groceryItems = new List<GroceryItemDto>();

            foreach (var required in requiredIngredients)
            {
                var ingredientId = required.Key;
                var requiredAmount = required.Value;
                var currentAmount = fridgeInventory.GetValueOrDefault(ingredientId, 0);

                if (currentAmount < requiredAmount)
                {
                    var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(ingredientId);
                    if (ingredient != null)
                    {
                        var earliestDate = ingredientFirstNeededDate.GetValueOrDefault(ingredientId, DateTime.MaxValue);
                        var isNeededSoon = earliestDate == today || earliestDate == tomorrow;

                        groceryItems.Add(new GroceryItemDto
                        {
                            IngredientId = ingredientId,
                            IngredientName = ingredient.IngredientName,
                            Unit = ingredient.Unit,
                            RequiredAmount = requiredAmount,
                            CurrentAmount = currentAmount,
                            NeededAmount = requiredAmount - currentAmount,
                            IsNeededSoon = isNeededSoon,
                            EarliestNeededDate = earliestDate
                        });
                    }
                }
            }

            var groceryList = new GroceryListDto
            {
                AccountId = accountId,
                MealPlanId = mealPlan.Id,
                MealPlanName = mealPlan.PlanName,
                GeneratedDate = DateTime.UtcNow,
                MissingIngredients = groceryItems
            };

            _logger.LogInformation("Grocery list generated for account {AccountId}, meal plan {PlanId} ({PlanName}) with {ItemCount} items", 
                accountId, mealPlan.Id, mealPlan.PlanName, groceryItems.Count);

            return groceryList;
        }

        private FridgeItemDto MapToDto(FridgeItem fridgeItem)
        {
            var now = DateTime.UtcNow.Date;
            var expiryDate = fridgeItem.ExpiryDate.Date;
            var daysUntilExpiry = (expiryDate - now).Days;

            return new FridgeItemDto
            {
                Id = fridgeItem.Id,
                AccountId = fridgeItem.AccountId,
                IngredientId = fridgeItem.IngredientId,
                IngredientName = fridgeItem.Ingredient?.IngredientName ?? string.Empty,
                Unit = fridgeItem.Ingredient?.Unit ?? string.Empty,
                CurrentAmount = fridgeItem.CurrentAmount,
                ExpiryDate = fridgeItem.ExpiryDate,
                IsExpiring = daysUntilExpiry <= 3 && daysUntilExpiry > 0,
                IsExpired = daysUntilExpiry <= 0
            };
        }
    }
}