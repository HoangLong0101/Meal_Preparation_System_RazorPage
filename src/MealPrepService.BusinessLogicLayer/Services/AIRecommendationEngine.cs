using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// AI-only recommendation engine using GPT for all recommendations
    /// </summary>
    public class AIRecommendationEngine : IRecommendationEngine
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILLMService _llmService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIRecommendationEngine> _logger;

        public AIRecommendationEngine(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AIRecommendationEngine> logger,
            ILLMService llmService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        }

        public async Task<List<MealRecommendation>> GenerateRecommendationsAsync(
            CustomerContext context,
            int minCount,
            int maxCount)
        {
            // Get all available recipes
            var allRecipes = await _unitOfWork.Recipes.GetAllAsync();
            var recipeList = allRecipes.ToList();

            if (!recipeList.Any())
            {
                _logger.LogError("No recipes available for recommendations");
                throw new InvalidOperationException("No recipes available in the database");
            }

            // Filter out recipes with allergens (safety first)
            var safeRecipes = await FilterAllergens(recipeList, context);

            if (!safeRecipes.Any())
            {
                _logger.LogError("No safe recipes found after allergen filtering for customer {CustomerId}", context.Customer.Id);
                throw new InvalidOperationException("No safe recipes available after filtering allergens");
            }

            // Check if AI is enabled
            var useAI = _configuration.GetValue<bool>("AI:UseRealAI", false);
            if (!useAI)
            {
                _logger.LogError("AI is disabled but this is an AI-only system");
                throw new InvalidOperationException("AI is disabled. This system requires AI to be enabled (AI:UseRealAI = true)");
            }

            // Check AI service availability
            var isAvailable = await _llmService.IsAvailableAsync();
            if (!isAvailable)
            {
                _logger.LogError("AI service is unavailable for customer {CustomerId}", context.Customer.Id);
                throw new InvalidOperationException("AI service is unavailable. Please check your OpenAI API key and configuration.");
            }

            // Generate AI recommendations
            _logger.LogInformation("Using AI-powered recommendations for customer {CustomerId}", context.Customer.Id);
            return await GenerateAIRecommendationsAsync(context, safeRecipes, maxCount);
        }

        public async Task<List<MealRecommendation>> GenerateRecommendationsAsync(
            CustomerContext context,
            int minCount,
            int maxCount,
            DateTime targetDate,
            string mealType)
        {
            // Get all available recipes
            var allRecipes = await _unitOfWork.Recipes.GetAllAsync();
            var recipeList = allRecipes.ToList();

            if (!recipeList.Any())
            {
                _logger.LogError("No recipes available for recommendations");
                throw new InvalidOperationException("No recipes available in the database");
            }

            // Filter out recipes with allergens (safety first)
            var safeRecipes = await FilterAllergens(recipeList, context);

            if (!safeRecipes.Any())
            {
                _logger.LogError("No safe recipes found after allergen filtering for customer {CustomerId}", context.Customer.Id);
                throw new InvalidOperationException("No safe recipes available after filtering allergens");
            }

            // Get recent recipe history (last 3 days)
            var threeDaysAgo = targetDate.AddDays(-3);
            var recentRecipeIds = await _unitOfWork.MealPlans.GetRecentRecipeIdsAsync(
                context.Customer.Id, 
                threeDaysAgo, 
                targetDate.AddDays(-1)); // Exclude today

            _logger.LogInformation("Found {Count} recent recipes to avoid for customer {CustomerId}", 
                recentRecipeIds.Count, context.Customer.Id);

            // Check if AI is enabled
            var useAI = _configuration.GetValue<bool>("AI:UseRealAI", false);
            if (!useAI)
            {
                _logger.LogError("AI is disabled but this is an AI-only system");
                throw new InvalidOperationException("AI is disabled. This system requires AI to be enabled (AI:UseRealAI = true)");
            }

            // Check AI service availability
            var isAvailable = await _llmService.IsAvailableAsync();
            if (!isAvailable)
            {
                _logger.LogError("AI service is unavailable for customer {CustomerId}", context.Customer.Id);
                throw new InvalidOperationException("AI service is unavailable. Please check your OpenAI API key and configuration.");
            }

            // Generate diversity-aware AI recommendations
            _logger.LogInformation("Using diversity-aware AI recommendations for customer {CustomerId}, {MealType} on {Date}", 
                context.Customer.Id, mealType, targetDate);
            return await GenerateDiversityAwareRecommendationsAsync(context, safeRecipes, maxCount, recentRecipeIds, targetDate, mealType);
        }

        private async Task<List<MealRecommendation>> GenerateDiversityAwareRecommendationsAsync(
            CustomerContext context,
            List<Recipe> safeRecipes,
            int maxCount,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType)
        {
            try
            {
                var aiResponse = await _llmService.GenerateRecommendationsAsync(
                    context, 
                    safeRecipes, 
                    maxCount, 
                    recentRecipeIds, 
                    targetDate, 
                    mealType);

                var recommendations = new List<MealRecommendation>();

                foreach (var aiRec in aiResponse.RecommendedRecipes)
                {
                    var recipe = safeRecipes.FirstOrDefault(r => r.Id == aiRec.RecipeId);
                    if (recipe != null)
                    {
                        recommendations.Add(new MealRecommendation
                        {
                            Recipe = recipe,
                            RelevanceScore = aiRec.ConfidenceScore * 100, // Convert to 0-100 scale
                            ReasoningExplanation = aiRec.Reasoning,
                            RecommendedRecipeIds = new List<Guid> { recipe.Id }, // Single recipe per recommendation
                            Date = targetDate,
                            MealType = mealType,
                            NutritionalInfo = new NutritionalInfo
                            {
                                TotalCalories = (decimal)recipe.TotalCalories,
                                ProteinG = (decimal)recipe.ProteinG,
                                FatG = (decimal)recipe.FatG,
                                CarbsG = (decimal)recipe.CarbsG
                            }
                        });
                    }
                }

                if (!recommendations.Any())
                {
                    _logger.LogError("AI returned no valid diversity-aware recommendations for customer {CustomerId}", context.Customer.Id);
                    throw new InvalidOperationException("AI service returned no valid recommendations");
                }

                _logger.LogInformation("Generated {Count} diversity-aware AI recommendations for customer {CustomerId}",
                    recommendations.Count, context.Customer.Id);

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diversity-aware AI recommendation generation failed for customer {CustomerId}", context.Customer.Id);
                throw new InvalidOperationException($"AI recommendation failed: {ex.Message}", ex);
            }
        }

        private async Task<List<MealRecommendation>> GenerateAIRecommendationsAsync(
            CustomerContext context,
            List<Recipe> safeRecipes,
            int maxCount)
        {
            try
            {
                var aiResponse = await _llmService.GenerateRecommendationsAsync(context, safeRecipes, maxCount);

                var recommendations = new List<MealRecommendation>();

                foreach (var aiRec in aiResponse.RecommendedRecipes)
                {
                    var recipe = safeRecipes.FirstOrDefault(r => r.Id == aiRec.RecipeId);
                    if (recipe != null)
                    {
                        recommendations.Add(new MealRecommendation
                        {
                            Recipe = recipe,
                            RelevanceScore = aiRec.ConfidenceScore * 100, // Convert to 0-100 scale
                            ReasoningExplanation = aiRec.Reasoning,
                            RecommendedRecipeIds = new List<Guid> { recipe.Id }, // Single recipe per recommendation
                            NutritionalInfo = new NutritionalInfo
                            {
                                TotalCalories = (decimal)recipe.TotalCalories,
                                ProteinG = (decimal)recipe.ProteinG,
                                FatG = (decimal)recipe.FatG,
                                CarbsG = (decimal)recipe.CarbsG
                            }
                        });
                    }
                }

                if (!recommendations.Any())
                {
                    _logger.LogError("AI returned no valid recommendations for customer {CustomerId}", context.Customer.Id);
                    throw new InvalidOperationException("AI service returned no valid recommendations");
                }

                _logger.LogInformation("Generated {Count} AI-powered recommendations for customer {CustomerId}",
                    recommendations.Count, context.Customer.Id);

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI recommendation generation failed for customer {CustomerId}", context.Customer.Id);
                throw new InvalidOperationException($"AI recommendation failed: {ex.Message}", ex);
            }
        }

        private async Task<List<Recipe>> FilterAllergens(List<Recipe> recipes, CustomerContext context)
        {
            if (!context.Allergies.Any())
            {
                return recipes;
            }

            var allergenNames = context.Allergies.Select(a => a.AllergyName).ToHashSet();
            var allIngredients = await _unitOfWork.Ingredients.GetAllAsync();
            var allergenIngredientIds = allIngredients
                .Where(i => i.IsAllergen && allergenNames.Contains(i.IngredientName))
                .Select(i => i.Id)
                .ToHashSet();

            if (!allergenIngredientIds.Any())
            {
                return recipes;
            }

            var safeRecipes = new List<Recipe>();

            foreach (var recipe in recipes)
            {
                var recipeIngredients = await _unitOfWork.RecipeIngredients
                    .GetByRecipeIdAsync(recipe.Id);

                var hasAllergen = recipeIngredients
                    .Any(ri => allergenIngredientIds.Contains(ri.IngredientId));

                if (!hasAllergen)
                {
                    safeRecipes.Add(recipe);
                }
            }

            _logger.LogInformation("Filtered {Original} recipes to {Safe} safe recipes (excluding {AllergenCount} allergens)",
                recipes.Count, safeRecipes.Count, allergenNames.Count);

            return safeRecipes;
        }
    }
}