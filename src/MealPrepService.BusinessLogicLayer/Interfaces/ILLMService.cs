using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Interface for Large Language Model services (Google Gemini)
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Generate meal recommendations using AI reasoning
        /// </summary>
        Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context, 
            List<Recipe> candidateRecipes,
            int maxRecommendations);

        /// <summary>
        /// Generate diverse meal recommendations that avoid recent recipes
        /// </summary>
        Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType);

        /// <summary>
        /// Generate natural language explanation for a recommendation
        /// </summary>
        Task<string> GenerateRecommendationReasoningAsync(
            Recipe recipe,
            CustomerContext context);

        /// <summary>
        /// Check if the LLM service is available and healthy
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get the current model being used
        /// </summary>
        string GetModelName();

        /// <summary>
        /// Generate new recipe suggestions based on user preferences and dietary requirements
        /// </summary>
        Task<List<AIGeneratedRecipe>> GenerateRecipeSuggestionsAsync(
            string cuisineType,
            string dietaryRestrictions,
            int calorieTarget,
            int count = 5);

        /// <summary>
        /// Generate daily meal suggestions based on user's current context and preferences
        /// </summary>
        Task<DailyMealSuggestionResponse> GenerateDailyMealSuggestionsAsync(
            CustomerContext context,
            DailyMealRequest request,
            List<Recipe> availableRecipes);

        /// <summary>
        /// Generate meal suggestions based on available inventory items, prioritizing items close to expiration
        /// </summary>
        Task<InventoryMealSuggestionResponse> GenerateInventoryBasedMealAsync(
            CustomerContext context,
            InventoryMealRequest request,
            List<Recipe> availableRecipes);
    }

    public class DailyMealRequest
    {
        public string MealType { get; set; } = string.Empty; // Breakfast, Lunch, Dinner, Snack
        public string PreparationMethod { get; set; } = string.Empty; // Cooking, Order
        public string? CurrentMood { get; set; }
        public int? Budget { get; set; }
        public int? AvailableTime { get; set; } // in minutes
        public string? SpecificRequest { get; set; }
        public bool PrioritizeHealthy { get; set; }
        public int NumberOfPeople { get; set; } = 1;
        public List<FamilyMemberInfo> FamilyMembers { get; set; } = new();
    }

    public class FamilyMemberInfo
    {
        public string Role { get; set; } = string.Empty; // e.g. "Father", "Mother", "Child (5 years)", "Elderly"
        public string? Note { get; set; } // e.g. "Diabetic", "Vegetarian", "Allergic to shrimp"
    }

    public class DailyMealSuggestionResponse
    {
        public List<DailyMealSuggestion> Suggestions { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
    }

    public class DailyMealSuggestion
    {
        public Guid? RecipeId { get; set; }
        public string MealName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float EstimatedCalories { get; set; }
        public float EstimatedPrice { get; set; }
        public int PrepTime { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public bool IsFromDatabase { get; set; }
        public string? Instructions { get; set; }
        public List<SuggestionIngredient>? Ingredients { get; set; }
        public float ProteinG { get; set; }
        public float CarbsG { get; set; }
        public float FatG { get; set; }
    }

    public class SuggestionIngredient
    {
        public string Name { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class AIRecommendationResponse
    {
        public List<AIRecommendedRecipe> RecommendedRecipes { get; set; } = new();
        public string OverallReasoning { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class AIRecommendedRecipe
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public List<string> MatchedCriteria { get; set; } = new();
    }

    public class AIGeneratedRecipe
    {
        public string RecipeName { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public float EstimatedCalories { get; set; }
        public float EstimatedProtein { get; set; }
        public float EstimatedCarbs { get; set; }
        public float EstimatedFat { get; set; }
        public List<RecipeIngredientSuggestion> Ingredients { get; set; } = new();
        public string CuisineType { get; set; } = string.Empty;
        public string DietaryInfo { get; set; } = string.Empty;
    }

    public class RecipeIngredientSuggestion
    {
        public string IngredientName { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    // Inventory-based meal suggestion DTOs
    public class InventoryMealRequest
    {
        public List<InventoryIngredient> AvailableIngredients { get; set; } = new();
        public string MealType { get; set; } = string.Empty; // Breakfast, Lunch, Dinner, Snack
        public int? MaxPrepTime { get; set; } // in minutes
        public bool PrioritizeExpiring { get; set; } = true;
        public bool GenerateShoppingList { get; set; } = true;
        public string? MealPreference { get; set; } // User's description of what they want to eat
    }

    public class InventoryIngredient
    {
        public Guid IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float AvailableAmount { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry => (ExpiryDate.Date - DateTime.Today).Days;
        public bool IsExpiringSoon => DaysUntilExpiry <= 3 && DaysUntilExpiry >= 0;
        public bool IsExpired => DaysUntilExpiry < 0;
    }

    public class InventoryMealSuggestionResponse
    {
        public List<InventoryMealSuggestion> Suggestions { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
        public List<string> UsedExpiringIngredients { get; set; } = new();
    }

    public class InventoryMealSuggestion
    {
        public Guid? RecipeId { get; set; }
        public string MealName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float EstimatedCalories { get; set; }
        public int PrepTime { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public bool IsFromDatabase { get; set; }
        public string? Instructions { get; set; }
        public float ProteinG { get; set; }
        public float CarbsG { get; set; }
        public float FatG { get; set; }
        public List<MealIngredientUsage> IngredientsFromInventory { get; set; } = new();
        public List<ShoppingListItem> MissingIngredients { get; set; } = new();
        public int InventoryMatchPercentage { get; set; } // How much of the recipe can be made from inventory
    }

    public class MealIngredientUsage
    {
        public Guid IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float RequiredAmount { get; set; }
        public float AvailableAmount { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool IsExpiringSoon { get; set; }
        public int DaysUntilExpiry { get; set; }
    }

    public class ShoppingListItem
    {
        public string IngredientName { get; set; } = string.Empty;
        public float NeededAmount { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool IsEssential { get; set; } = true; // Essential for the recipe
        public string? SubstituteNote { get; set; } // Optional note about substitutes
    }
}
