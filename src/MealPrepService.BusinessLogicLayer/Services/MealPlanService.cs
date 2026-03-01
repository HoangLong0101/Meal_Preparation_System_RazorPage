using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class MealPlanService : IMealPlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MealPlanService> _logger;
        private readonly IAIRecommendationService _aiRecommendationService;

        public MealPlanService(
            IUnitOfWork unitOfWork,
            ILogger<MealPlanService> logger,
            IAIRecommendationService aiRecommendationService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiRecommendationService = aiRecommendationService ?? throw new ArgumentNullException(nameof(aiRecommendationService));
        }

        public async Task<MealPlanDto> CreateManualMealPlanAsync(MealPlanDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.PlanName))
            {
                throw new BusinessException("Plan name is required");
            }

            // Validate date range
            if (dto.EndDate < dto.StartDate)
            {
                throw new BusinessException("End date must be after start date");
            }

            // Create meal plan entity
            var mealPlan = new MealPlan
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                PlanName = dto.PlanName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsAiGenerated = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MealPlans.AddAsync(mealPlan);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Manual meal plan created: {PlanName} for account {AccountId}", 
                dto.PlanName, dto.AccountId);

            return MapToDto(mealPlan);
        }

        public async Task AddMealToPlanAsync(Guid planId, MealDto mealDto)
        {
            if (mealDto == null)
            {
                throw new ArgumentNullException(nameof(mealDto));
            }

            // Get the meal plan
            var mealPlan = await _unitOfWork.MealPlans.GetByIdAsync(planId);
            if (mealPlan == null)
            {
                throw new BusinessException($"Meal plan with ID {planId} not found");
            }

            // Validate meal type
            if (string.IsNullOrWhiteSpace(mealDto.MealType))
            {
                throw new BusinessException("Meal type is required");
            }

            var validMealTypes = new[] { "breakfast", "lunch", "dinner", "snack" };
            if (!validMealTypes.Contains(mealDto.MealType.ToLower()))
            {
                throw new BusinessException("Meal type must be breakfast, lunch, dinner, or snack");
            }

            // Validate serve date is within plan date range
            if (mealDto.ServeDate < mealPlan.StartDate || mealDto.ServeDate > mealPlan.EndDate)
            {
                throw new BusinessException($"Serve date must be within plan date range ({mealPlan.StartDate:yyyy-MM-dd} to {mealPlan.EndDate:yyyy-MM-dd})");
            }

            // Check if a meal of the same type already exists for this date
            var existingMeal = (await _unitOfWork.Meals.GetAllAsync())
                .FirstOrDefault(m => 
                    m.PlanId == planId && 
                    m.MealType.ToLower() == mealDto.MealType.ToLower() && 
                    m.ServeDate.Date == mealDto.ServeDate.Date);

            Meal meal;
            bool isNewMeal = false;

            if (existingMeal != null)
            {
                // Use existing meal
                meal = existingMeal;
                _logger.LogInformation("Found existing {MealType} meal for {ServeDate}, adding recipes to it", 
                    meal.MealType, meal.ServeDate);
            }
            else
            {
                // Create new meal entity
                meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    PlanId = planId,
                    MealType = mealDto.MealType.ToLower(),
                    ServeDate = mealDto.ServeDate,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Meals.AddAsync(meal);
                isNewMeal = true;
                _logger.LogInformation("Creating new {MealType} meal for {ServeDate}", 
                    meal.MealType, meal.ServeDate);
            }

            // Add recipe links if provided
            if (mealDto.Recipes != null && mealDto.Recipes.Any())
            {
                // Get existing recipe IDs for this meal to avoid duplicates
                var existingRecipeIds = _unitOfWork.MealRecipes
                    .Where(mr => mr.MealId == meal.Id)
                    .Select(mr => mr.RecipeId)
                    .ToHashSet();

                int addedCount = 0;
                int skippedCount = 0;

                foreach (var recipeDto in mealDto.Recipes)
                {
                    // Skip if recipe is already in this meal
                    if (existingRecipeIds.Contains(recipeDto.Id))
                    {
                        skippedCount++;
                        _logger.LogInformation("Recipe {RecipeId} already exists in meal, skipping", recipeDto.Id);
                        continue;
                    }

                    // Verify recipe exists
                    var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeDto.Id);
                    if (recipe == null)
                    {
                        throw new BusinessException($"Recipe with ID {recipeDto.Id} not found");
                    }

                    var mealRecipe = new MealRecipe
                    {
                        MealId = meal.Id,
                        RecipeId = recipeDto.Id
                    };

                    await _unitOfWork.MealRecipes.AddAsync(mealRecipe);
                    addedCount++;
                }

                _logger.LogInformation("Added {AddedCount} recipe(s) to meal, skipped {SkippedCount} duplicate(s)", 
                    addedCount, skippedCount);
            }

            await _unitOfWork.SaveChangesAsync();

            if (isNewMeal)
            {
                _logger.LogInformation("New meal added to plan {PlanId}: {MealType} on {ServeDate}", 
                    planId, meal.MealType, meal.ServeDate);
            }
            else
            {
                _logger.LogInformation("Recipes added to existing meal in plan {PlanId}: {MealType} on {ServeDate}", 
                    planId, meal.MealType, meal.ServeDate);
            }
        }

        public async Task<MealPlanDto?> GetByIdAsync(Guid planId)
        {
            var mealPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(planId);

            if (mealPlan == null)
            {
                return null;
            }

            return MapToDto(mealPlan);
        }

        public async Task<IEnumerable<MealPlanDto>> GetByAccountIdAsync(Guid accountId)
        {
            var mealPlans = await _unitOfWork.MealPlans.GetByAccountIdAsync(accountId);

            var dtos = new List<MealPlanDto>();
            foreach (var plan in mealPlans)
            {
                // Get full plan with meals and recipes
                var fullPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(plan.Id);
                if (fullPlan != null)
                {
                    dtos.Add(MapToDto(fullPlan));
                }
            }

            return dtos;
        }

        public async Task DeleteAsync(Guid planId, Guid requestingAccountId)
        {
            // 1. Retrieve meal plan
            var mealPlan = await _unitOfWork.MealPlans.GetByIdAsync(planId);
            
            if (mealPlan == null)
            {
                throw new NotFoundException($"Meal plan with ID {planId} not found");
            }
            
            // 2. Authorization check (service layer)
            // Note: Controller will also check, but defense in depth
            var requestingAccount = await _unitOfWork.Accounts.GetByIdAsync(requestingAccountId);
            
            if (requestingAccount == null)
            {
                throw new AuthenticationException("Requesting account not found");
            }
            
            // Check if user owns the plan or is a manager
            if (mealPlan.AccountId != requestingAccountId && requestingAccount.Role != "Manager")
            {
                throw new AuthorizationException("You don't have permission to delete this meal plan");
            }
            
            // 3. Delete the meal plan (cascade will handle meals and meal-recipes)
            await _unitOfWork.MealPlans.DeleteAsync(planId);
            await _unitOfWork.SaveChangesAsync();
            
            // 4. Log the deletion
            _logger.LogInformation(
                "Meal plan {PlanId} ({PlanName}) deleted by account {AccountId}",
                planId, mealPlan.PlanName, requestingAccountId);
        }

        public async Task<MealPlanDto?> GetActivePlanAsync(Guid accountId)
        {
            var plans = await _unitOfWork.MealPlans.GetByAccountIdAsync(accountId);
            var activePlan = plans.FirstOrDefault(p => p.IsActive);

            if (activePlan == null)
            {
                return null;
            }

            // Get full plan with meals and recipes
            var fullPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(activePlan.Id);
            return fullPlan != null ? MapToDto(fullPlan) : null;
        }

        public async Task SetActivePlanAsync(Guid planId, Guid accountId)
        {
            // Get the plan to activate
            var planToActivate = await _unitOfWork.MealPlans.GetByIdAsync(planId);
            
            if (planToActivate == null)
            {
                throw new NotFoundException($"Meal plan with ID {planId} not found");
            }

            // Verify ownership
            if (planToActivate.AccountId != accountId)
            {
                throw new AuthorizationException("You don't have permission to modify this meal plan");
            }

            // Deactivate all other plans for this account
            var allPlans = await _unitOfWork.MealPlans.GetByAccountIdAsync(accountId);
            foreach (var plan in allPlans)
            {
                if (plan.IsActive)
                {
                    plan.IsActive = false;
                    await _unitOfWork.MealPlans.UpdateAsync(plan);
                }
            }

            // Activate the selected plan
            planToActivate.IsActive = true;
            await _unitOfWork.MealPlans.UpdateAsync(planToActivate);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Meal plan {PlanId} ({PlanName}) set as active for account {AccountId}", 
                planId, planToActivate.PlanName, accountId);
        }

        public async Task RemoveRecipeFromMealAsync(Guid mealId, Guid recipeId, Guid accountId)
        {
            // Get the meal with its plan
            var meal = await _unitOfWork.Meals.GetByIdAsync(mealId);
            if (meal == null)
            {
                throw new NotFoundException($"Meal with ID {mealId} not found");
            }

            // Get the meal plan to verify ownership
            var mealPlan = await _unitOfWork.MealPlans.GetByIdAsync(meal.PlanId);
            if (mealPlan == null)
            {
                throw new NotFoundException($"Meal plan not found");
            }

            // Verify ownership
            if (mealPlan.AccountId != accountId)
            {
                throw new AuthorizationException("You don't have permission to modify this meal plan");
            }

            // Find the MealRecipe junction record
            var mealRecipe = _unitOfWork.MealRecipes
                .Where(mr => mr.MealId == mealId && mr.RecipeId == recipeId)
                .FirstOrDefault();

            if (mealRecipe == null)
            {
                throw new NotFoundException($"Recipe not found in this meal");
            }

            // Check if this is the last recipe in the meal
            var recipeCount = _unitOfWork.MealRecipes
                .Where(mr => mr.MealId == mealId)
                .Count();

            if (recipeCount <= 1)
            {
                // Delete the entire meal if it's the last recipe
                await _unitOfWork.Meals.DeleteAsync(mealId);
                _logger.LogInformation("Deleted meal {MealId} as it was the last recipe", mealId);
            }
            else
            {
                // Just remove the recipe from the meal
                _unitOfWork.MealRecipes.Remove(mealRecipe);
                _logger.LogInformation("Removed recipe {RecipeId} from meal {MealId}", recipeId, mealId);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<MealPlanDto> GenerateAiMealPlanAsync(Guid accountId, DateTime startDate, DateTime endDate)
        {
            return await GenerateAiMealPlanAsync(accountId, startDate, endDate, null);
        }

        public async Task<MealPlanDto> GenerateAiMealPlanAsync(Guid accountId, DateTime startDate, DateTime endDate, string? customPlanName)
        {
            // Validate date range
            if (endDate < startDate)
            {
                throw new BusinessException("End date must be after start date");
            }

            _logger.LogInformation("Starting AI meal plan generation for account {AccountId} from {StartDate} to {EndDate}", 
                accountId, startDate, endDate);

            // Use custom name if provided, otherwise generate default name
            var planName = !string.IsNullOrWhiteSpace(customPlanName) 
                ? customPlanName 
                : $"AI Meal Plan {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";

            // Create meal plan entity first
            var mealPlan = new MealPlan
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanName = planName,
                StartDate = startDate,
                EndDate = endDate,
                IsAiGenerated = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MealPlans.AddAsync(mealPlan);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                // Get AI recommendations for the entire meal plan
                var recommendations = await _aiRecommendationService.GetMealPlanRecommendationsAsync(
                    accountId, 
                    startDate, 
                    endDate);

                if (recommendations == null || !recommendations.Any())
                {
                    _logger.LogError("No AI recommendations returned for account {AccountId}", accountId);
                    throw new InvalidOperationException("AI service returned no recommendations. Please check your AI configuration and try again.");
                }

                _logger.LogInformation("Received {Count} AI recommendations, creating meals", recommendations.Count());
                await CreateMealsFromRecommendations(mealPlan, recommendations);
            }
            catch (InvalidOperationException)
            {
                // Re-throw AI-specific exceptions without modification
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI meal generation for account {AccountId}", accountId);
                throw new InvalidOperationException($"AI meal plan generation failed: {ex.Message}", ex);
            }

            _logger.LogInformation("AI meal plan generated successfully for account {AccountId}", accountId);

            // Return the full meal plan with meals and recipes
            var fullPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(mealPlan.Id);
            return MapToDto(fullPlan!);
        }

        private async Task CreateMealsFromRecommendations(MealPlan mealPlan, IEnumerable<MealRecommendation> recommendations)
        {
            foreach (var recommendation in recommendations)
            {
                // Create meal
                var meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    PlanId = mealPlan.Id,
                    MealType = recommendation.MealType.ToLower(),
                    ServeDate = recommendation.Date,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Meals.AddAsync(meal);
                await _unitOfWork.SaveChangesAsync(); // Save meal first to get ID

                // Add recommended recipes to the meal
                foreach (var recipeId in recommendation.RecommendedRecipeIds)
                {
                    var mealRecipe = new MealRecipe
                    {
                        MealId = meal.Id,
                        RecipeId = recipeId
                    };

                    await _unitOfWork.MealRecipes.AddAsync(mealRecipe);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created {MealType} meal for {Date} with {RecipeCount} recipes (AI recommended)", 
                    meal.MealType, meal.ServeDate, recommendation.RecommendedRecipeIds.Count);
            }
        }

        private MealPlanDto MapToDto(MealPlan mealPlan)
        {
            var dto = new MealPlanDto
            {
                Id = mealPlan.Id,
                AccountId = mealPlan.AccountId,
                PlanName = mealPlan.PlanName,
                StartDate = mealPlan.StartDate,
                EndDate = mealPlan.EndDate,
                IsAiGenerated = mealPlan.IsAiGenerated,
                IsActive = mealPlan.IsActive,
                Meals = new List<MealDto>()
            };

            if (mealPlan.Meals != null && mealPlan.Meals.Any())
            {
                dto.Meals = mealPlan.Meals.Select(MapMealToDto).ToList();
            }

            return dto;
        }

        private MealDto MapMealToDto(Meal meal)
        {
            var dto = new MealDto
            {
                Id = meal.Id,
                PlanId = meal.PlanId,
                MealType = meal.MealType,
                ServeDate = meal.ServeDate,
                MealFinished = meal.MealFinished,
                Recipes = new List<RecipeDto>()
            };

            if (meal.MealRecipes != null && meal.MealRecipes.Any())
            {
                dto.Recipes = meal.MealRecipes
                    .Where(mr => mr.Recipe != null)
                    .Select(mr => MapRecipeToDto(mr.Recipe))
                    .ToList();
            }

            return dto;
        }


        private RecipeDto MapRecipeToDto(Recipe recipe)
        {
            return new RecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                // Include nutrition data from existing recipe data
                TotalCalories = recipe.TotalCalories,
                ProteinG = recipe.ProteinG,
                FatG = recipe.FatG,
                CarbsG = recipe.CarbsG,
                // Map ingredients with amounts and units
                Ingredients = recipe.RecipeIngredients?.Select(ri => new RecipeIngredientDto
                {
                    IngredientId = ri.IngredientId,
                    IngredientName = ri.Ingredient?.IngredientName ?? "Unknown Ingredient",
                    Amount = ri.Amount,
                    Unit = ri.Ingredient?.Unit ?? "unit",
                    CaloPerUnit = ri.Ingredient?.CaloPerUnit ?? 0,
                    IsAllergen = ri.Ingredient?.IsAllergen ?? false
                }).ToList() ?? new List<RecipeIngredientDto>()
            };
        }

        public async Task MarkMealAsFinishedAsync(Guid mealId, Guid accountId, bool finished)
        {
            var meal = await _unitOfWork.Meals.GetByIdAsync(mealId);
            if (meal == null)
            {
                throw new NotFoundException($"Meal with ID {mealId} not found");
            }

            // Verify the meal belongs to the account
            var mealPlan = await _unitOfWork.MealPlans.GetByIdAsync(meal.PlanId);
            if (mealPlan == null || mealPlan.AccountId != accountId)
            {
                throw new AuthorizationException("You don't have permission to modify this meal");
            }

            meal.MealFinished = finished;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Meal {MealId} marked as {Status} by account {AccountId}", 
                mealId, finished ? "finished" : "not finished", accountId);
        }
    }
}