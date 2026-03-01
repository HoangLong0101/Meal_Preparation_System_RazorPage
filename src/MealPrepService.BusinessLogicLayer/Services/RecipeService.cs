using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// Service for recipe management operations
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RecipeService> _logger;
        private readonly ILLMService _llmService;
        private readonly IHealthProfileService _healthProfileService;

        public RecipeService(
            IUnitOfWork unitOfWork, 
            ILogger<RecipeService> logger,
            ILLMService llmService,
            IHealthProfileService healthProfileService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _llmService = llmService;
            _healthProfileService = healthProfileService;
        }

        public async Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto)
        {
            _logger.LogInformation("Creating recipe with name: {RecipeName}", dto.RecipeName);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.RecipeName))
            {
                throw new BusinessException("Recipe name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Instructions))
            {
                throw new BusinessException("Recipe instructions are required");
            }

            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                RecipeName = dto.RecipeName.Trim(),
                Instructions = dto.Instructions.Trim(),
                TotalCalories = dto.TotalCalories,
                ProteinG = dto.ProteinG,
                FatG = dto.FatG,
                CarbsG = dto.CarbsG,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Recipes.AddAsync(recipe);
            await _unitOfWork.SaveChangesAsync();

            // Add ingredients if provided
            if (dto.Ingredients != null && dto.Ingredients.Any())
            {
                _logger.LogInformation("Adding {Count} ingredients to recipe {RecipeId}", dto.Ingredients.Count, recipe.Id);
                
                // Calculate calories per ingredient based on recipe total
                var totalIngredients = dto.Ingredients.Count;
                var estimatedCaloriesPerIngredient = dto.TotalCalories / Math.Max(totalIngredients, 1);
                
                foreach (var ingredientDto in dto.Ingredients)
                {
                    if (string.IsNullOrWhiteSpace(ingredientDto.IngredientName))
                        continue;

                    // Find or create the ingredient
                    var existingIngredients = await _unitOfWork.Ingredients.FindAsync(
                        i => i.IngredientName.ToLower() == ingredientDto.IngredientName.ToLower().Trim());
                    
                    var ingredient = existingIngredients.FirstOrDefault();
                    
                    var ingredientName = ingredientDto.IngredientName.Trim();
                    var unit = ingredientDto.Unit ?? "unit";
                    
                    // Calculate CaloPerUnit based on quantity and estimated calories
                    float caloPerUnit = 0;
                    if (ingredientDto.Quantity > 0)
                    {
                        caloPerUnit = estimatedCaloriesPerIngredient / ingredientDto.Quantity;
                    }
                    
                    // Detect if ingredient is a common allergen
                    bool isAllergen = IsCommonAllergen(ingredientName);
                    
                    if (ingredient == null)
                    {
                        // Create new ingredient
                        ingredient = new Ingredient
                        {
                            Id = Guid.NewGuid(),
                            IngredientName = ingredientName,
                            Unit = unit,
                            CaloPerUnit = caloPerUnit,
                            IsAllergen = isAllergen,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Ingredients.AddAsync(ingredient);
                        await _unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("Created new ingredient: {IngredientName}, CaloPerUnit: {CaloPerUnit}, IsAllergen: {IsAllergen}", 
                            ingredient.IngredientName, ingredient.CaloPerUnit, ingredient.IsAllergen);
                    }
                    else
                    {
                        // Update existing ingredient if CaloPerUnit is 0 or IsAllergen needs to be set
                        bool needsUpdate = false;
                        
                        if (ingredient.CaloPerUnit == 0 && caloPerUnit > 0)
                        {
                            ingredient.CaloPerUnit = caloPerUnit;
                            needsUpdate = true;
                            _logger.LogInformation("Updated CaloPerUnit for existing ingredient: {IngredientName} -> {CaloPerUnit}", 
                                ingredient.IngredientName, caloPerUnit);
                        }
                        
                        if (!ingredient.IsAllergen && isAllergen)
                        {
                            ingredient.IsAllergen = isAllergen;
                            needsUpdate = true;
                            _logger.LogInformation("Updated IsAllergen for existing ingredient: {IngredientName} -> {IsAllergen}", 
                                ingredient.IngredientName, isAllergen);
                        }
                        
                        if (needsUpdate)
                        {
                            ingredient.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.Ingredients.UpdateAsync(ingredient);
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }

                    // Create recipe-ingredient relationship
                    var recipeIngredient = new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        IngredientId = ingredient.Id,
                        Amount = ingredientDto.Quantity
                    };
                    await _unitOfWork.RecipeIngredients.AddAsync(recipeIngredient);
                }
                
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Added ingredients to recipe {RecipeId}", recipe.Id);
            }

            _logger.LogInformation("Recipe created successfully with ID: {RecipeId}", recipe.Id);

            return MapToDto(recipe);
        }

        public async Task<RecipeDto> GetByIdAsync(Guid recipeId)
        {
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            return MapToDto(recipe);
        }

        public async Task<IEnumerable<RecipeDto>> GetAllAsync()
        {
            var recipes = await _unitOfWork.Recipes.GetAllAsync();
            return recipes.Select(MapToDto);
        }

        public async Task<IEnumerable<RecipeDto>> GetAllWithIngredientsAsync()
        {
            var recipes = await _unitOfWork.Recipes.GetAllWithIngredientsAsync();
            return recipes.Select(MapToDtoWithIngredients);
        }

        public async Task<RecipeDto> UpdateRecipeAsync(Guid recipeId, UpdateRecipeDto dto)
        {
            _logger.LogInformation("Updating recipe with ID: {RecipeId}", recipeId);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.RecipeName))
            {
                throw new BusinessException("Recipe name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Instructions))
            {
                throw new BusinessException("Recipe instructions are required");
            }

            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            recipe.RecipeName = dto.RecipeName.Trim();
            recipe.Instructions = dto.Instructions.Trim();
            recipe.UpdatedAt = DateTime.UtcNow;

            // NOTE: Nutrition recalculation is deferred as per task requirements
            
            await _unitOfWork.Recipes.UpdateAsync(recipe);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe updated successfully with ID: {RecipeId}", recipeId);

            return MapToDto(recipe);
        }

        public async Task DeleteRecipeAsync(Guid recipeId)
        {
            _logger.LogInformation("Deleting recipe with ID: {RecipeId}", recipeId);

            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            // Check if recipe is used in active menu meals
            var isUsedInActiveMenu = await _unitOfWork.Recipes.IsUsedInActiveMenuAsync(recipeId);
            
            if (isUsedInActiveMenu)
            {
                throw new BusinessException("Cannot delete recipe that is used in active menu meals");
            }

            await _unitOfWork.Recipes.DeleteAsync(recipeId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recipe deleted successfully with ID: {RecipeId}", recipeId);
        }

        public async Task AddIngredientToRecipeAsync(Guid recipeId, RecipeIngredientDto ingredientDto)
        {
            _logger.LogInformation("Adding ingredient {IngredientId} to recipe {RecipeId}", 
                ingredientDto.IngredientId, recipeId);

            // Validate required fields
            if (ingredientDto.IngredientId == Guid.Empty)
            {
                throw new BusinessException("Ingredient ID is required");
            }

            if (ingredientDto.Amount <= 0)
            {
                throw new BusinessException("Ingredient amount must be positive");
            }

            // Verify recipe exists
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            if (recipe == null)
            {
                throw new BusinessException("Recipe not found");
            }

            // Verify ingredient exists
            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(ingredientDto.IngredientId);
            if (ingredient == null)
            {
                throw new BusinessException("Ingredient not found");
            }

            // Check if ingredient is already in recipe
            var existingRecipeIngredient = await _unitOfWork.RecipeIngredients
                .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.IngredientId == ingredientDto.IngredientId);

            if (existingRecipeIngredient != null)
            {
                throw new BusinessException("Ingredient is already added to this recipe");
            }

            var recipeIngredient = new RecipeIngredient
            {
                RecipeId = recipeId,
                IngredientId = ingredientDto.IngredientId,
                Amount = ingredientDto.Amount
            };

            _unitOfWork.RecipeIngredients.Add(recipeIngredient);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ingredient added successfully to recipe");
        }

        public async Task<IEnumerable<RecipeDto>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds)
        {
            var recipes = await _unitOfWork.Recipes.GetByIngredientsAsync(ingredientIds);
            return recipes.Select(MapToDto);
        }

        public async Task<IEnumerable<RecipeDto>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds)
        {
            var recipes = await _unitOfWork.Recipes.GetExcludingAllergensAsync(allergyIds);
            return recipes.Select(MapToDto);
        }

        private static RecipeDto MapToDto(Recipe recipe)
        {
            return new RecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                TotalCalories = recipe.TotalCalories,
                ProteinG = recipe.ProteinG,
                FatG = recipe.FatG,
                CarbsG = recipe.CarbsG
            };
        }

        private static RecipeDto MapToDtoWithIngredients(Recipe recipe)
        {
            return new RecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                TotalCalories = recipe.TotalCalories,
                ProteinG = recipe.ProteinG,
                FatG = recipe.FatG,
                CarbsG = recipe.CarbsG,
                Ingredients = recipe.RecipeIngredients?.Select(ri => new RecipeIngredientDto
                {
                    IngredientId = ri.IngredientId,
                    IngredientName = ri.Ingredient?.IngredientName ?? string.Empty,
                    Amount = ri.Amount,
                    Unit = ri.Ingredient?.Unit ?? string.Empty,
                    CaloPerUnit = ri.Ingredient?.CaloPerUnit ?? 0,
                    IsAllergen = ri.Ingredient?.IsAllergen ?? false
                }).ToList() ?? new List<RecipeIngredientDto>()
            };
        }

        public async Task<List<RecipeDto>> GenerateAndSaveAIRecipeAsync(Guid userId, string cuisineType = "Vietnamese", int count = 3)
        {
            _logger.LogInformation("Generating and saving {Count} AI recipes for user {UserId} with cuisine type {CuisineType}", 
                count, userId, cuisineType);

            try
            {
                // 1. Get CustomerContext (Health Profile, Allergies)
                var healthProfile = await _healthProfileService.GetByAccountIdAsync(userId);
                if (healthProfile == null)
                {
                    throw new BusinessException("Health profile not found for user");
                }

                // Build CustomerContext
                var customer = await _unitOfWork.Accounts.GetByIdAsync(userId);
                if (customer == null)
                {
                    throw new BusinessException("User account not found");
                }

                var allergies = healthProfile.Allergies ?? new List<string>();
                var dietaryRestrictions = string.Join(", ", allergies);
                if (string.IsNullOrEmpty(dietaryRestrictions))
                {
                    dietaryRestrictions = "None";
                }

                // Calculate calorie target per meal (default to 2000 calories daily if not set)
                int dailyCalories = healthProfile.CalorieGoal ?? 2000;
                int calorieTarget = dailyCalories / 3; // Average per meal

                _logger.LogInformation("Calling AI service to generate recipes. Dietary restrictions: {Restrictions}, Calorie target: {Calories}",
                    dietaryRestrictions, calorieTarget);

                // 2. Call AI service to generate recipes
                var aiRecipes = await _llmService.GenerateRecipeSuggestionsAsync(
                    cuisineType,
                    dietaryRestrictions,
                    calorieTarget,
                    count);

                if (aiRecipes == null || !aiRecipes.Any())
                {
                    _logger.LogWarning("No recipes generated by AI service");
                    throw new BusinessException("Failed to generate recipes from AI service");
                }

                _logger.LogInformation("AI service returned {Count} recipes", aiRecipes.Count);

                // 3. Map and save recipes to database
                var savedRecipes = new List<RecipeDto>();
                var allIngredients = await _unitOfWork.Ingredients.GetAllAsync();
                var ingredientDict = allIngredients.ToDictionary(i => i.IngredientName.ToLower().Trim(), i => i);

                foreach (var aiRecipe in aiRecipes)
                {
                    try
                    {
                        // Create Recipe entity
                        var recipe = new Recipe
                        {
                            Id = Guid.NewGuid(), // Explicit GUID generation
                            RecipeName = aiRecipe.RecipeName.Trim(),
                            Instructions = aiRecipe.Instructions.Trim(),
                            TotalCalories = aiRecipe.EstimatedCalories,
                            ProteinG = aiRecipe.EstimatedProtein,
                            CarbsG = aiRecipe.EstimatedCarbs,
                            FatG = aiRecipe.EstimatedFat,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Recipes.AddAsync(recipe);
                        await _unitOfWork.SaveChangesAsync();

                        _logger.LogInformation("Recipe saved: {RecipeName} (ID: {RecipeId})", recipe.RecipeName, recipe.Id);

                        // 4. Map and save ingredients
                        if (aiRecipe.Ingredients != null && aiRecipe.Ingredients.Any())
                        {
                            // Calculate estimated calories per ingredient
                            var totalIngredients = aiRecipe.Ingredients.Count;
                            var estimatedCaloriesPerIngredient = aiRecipe.EstimatedCalories / Math.Max(totalIngredients, 1);

                            foreach (var aiIngredient in aiRecipe.Ingredients)
                            {
                                var ingredientKey = aiIngredient.IngredientName.ToLower().Trim();
                                
                                // Calculate CaloPerUnit based on quantity and estimated calories
                                float caloPerUnit = 0;
                                if (aiIngredient.Quantity > 0)
                                {
                                    caloPerUnit = estimatedCaloriesPerIngredient / aiIngredient.Quantity;
                                }

                                // Detect if ingredient is a common allergen
                                bool isAllergen = IsCommonAllergen(aiIngredient.IngredientName);
                                
                                // Try to find existing ingredient
                                if (ingredientDict.TryGetValue(ingredientKey, out var existingIngredient))
                                {
                                    // Update existing ingredient if CaloPerUnit is 0 or IsAllergen needs to be set
                                    bool needsUpdate = false;
                                    
                                    if (existingIngredient.CaloPerUnit == 0 && caloPerUnit > 0)
                                    {
                                        existingIngredient.CaloPerUnit = caloPerUnit;
                                        needsUpdate = true;
                                        _logger.LogInformation("Updated CaloPerUnit for existing ingredient: {IngredientName} -> {CaloPerUnit}", 
                                            existingIngredient.IngredientName, caloPerUnit);
                                    }
                                    
                                    if (!existingIngredient.IsAllergen && isAllergen)
                                    {
                                        existingIngredient.IsAllergen = isAllergen;
                                        needsUpdate = true;
                                        _logger.LogInformation("Updated IsAllergen for existing ingredient: {IngredientName} -> {IsAllergen}", 
                                            existingIngredient.IngredientName, isAllergen);
                                    }
                                    
                                    if (needsUpdate)
                                    {
                                        existingIngredient.UpdatedAt = DateTime.UtcNow;
                                        await _unitOfWork.Ingredients.UpdateAsync(existingIngredient);
                                        await _unitOfWork.SaveChangesAsync();
                                    }
                                    
                                    var recipeIngredient = new RecipeIngredient
                                    {
                                        RecipeId = recipe.Id,
                                        IngredientId = existingIngredient.Id,
                                        Amount = aiIngredient.Quantity
                                    };

                                    _unitOfWork.RecipeIngredients.Add(recipeIngredient);
                                    _logger.LogInformation("Linked existing ingredient: {IngredientName}", existingIngredient.IngredientName);
                                }
                                else
                                {
                                    // Create new ingredient if not exists
                                    var newIngredient = new Ingredient
                                    {
                                        Id = Guid.NewGuid(),
                                        IngredientName = aiIngredient.IngredientName.Trim(),
                                        Unit = aiIngredient.Unit.Trim(),
                                        CaloPerUnit = caloPerUnit,
                                        IsAllergen = isAllergen,
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    await _unitOfWork.Ingredients.AddAsync(newIngredient);
                                    await _unitOfWork.SaveChangesAsync();

                                    // Add to dictionary for future lookups
                                    ingredientDict[ingredientKey] = newIngredient;

                                    var recipeIngredient = new RecipeIngredient
                                    {
                                        RecipeId = recipe.Id,
                                        IngredientId = newIngredient.Id,
                                        Amount = aiIngredient.Quantity
                                    };

                                    _unitOfWork.RecipeIngredients.Add(recipeIngredient);
                                    _logger.LogInformation("Created new ingredient: {IngredientName}, CaloPerUnit: {CaloPerUnit}, IsAllergen: {IsAllergen}", 
                                        newIngredient.IngredientName, newIngredient.CaloPerUnit, newIngredient.IsAllergen);
                                }
                            }

                            await _unitOfWork.SaveChangesAsync();
                            _logger.LogInformation("Saved {Count} ingredients for recipe {RecipeName}", 
                                aiRecipe.Ingredients.Count, recipe.RecipeName);
                        }

                        savedRecipes.Add(MapToDto(recipe));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save recipe: {RecipeName}", aiRecipe.RecipeName);
                        // Continue with next recipe
                    }
                }

                _logger.LogInformation("Successfully saved {Count} AI-generated recipes to database", savedRecipes.Count);
                return savedRecipes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate and save AI recipes for user {UserId}", userId);
                throw new BusinessException("Failed to generate AI recipes: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Checks if an ingredient name matches common food allergens
        /// </summary>
        private static bool IsCommonAllergen(string ingredientName)
        {
            if (string.IsNullOrWhiteSpace(ingredientName))
                return false;

            var name = ingredientName.ToLower().Trim();

            // Common allergens based on the "Big 8" plus additional common allergens
            var allergenKeywords = new[]
            {
                // Milk/Dairy
                "milk", "cheese", "butter", "cream", "yogurt", "yoghurt", "dairy", "lactose", "whey", "casein",
                // Eggs
                "egg", "eggs", "mayonnaise", "mayo",
                // Fish
                "fish", "salmon", "tuna", "cod", "tilapia", "mackerel", "sardine", "anchovy", "bass", "trout",
                // Shellfish/Crustaceans
                "shrimp", "prawn", "crab", "lobster", "oyster", "mussel", "clam", "scallop", "shellfish", "crustacean",
                // Tree Nuts
                "almond", "cashew", "walnut", "pecan", "pistachio", "macadamia", "hazelnut", "chestnut", "brazil nut",
                // Peanuts
                "peanut", "groundnut",
                // Wheat/Gluten
                "wheat", "flour", "bread", "pasta", "noodle", "gluten", "semolina", "couscous",
                // Soy
                "soy", "soya", "tofu", "tempeh", "edamame", "miso",
                // Sesame
                "sesame", "tahini",
                // Additional common allergens
                "mustard", "celery", "lupin", "mollusc", "sulfite", "sulphite"
            };

            foreach (var keyword in allergenKeywords)
            {
                if (name.Contains(keyword))
                    return true;
            }

            return false;
        }
    }
}