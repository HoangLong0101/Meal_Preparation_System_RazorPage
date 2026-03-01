using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Service interface for recipe management operations
    /// </summary>
    public interface IRecipeService
    {
        Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto);
        Task<RecipeDto> GetByIdAsync(Guid recipeId);
        Task<IEnumerable<RecipeDto>> GetAllAsync();
        Task<IEnumerable<RecipeDto>> GetAllWithIngredientsAsync();
        Task<RecipeDto> UpdateRecipeAsync(Guid recipeId, UpdateRecipeDto dto);
        Task DeleteRecipeAsync(Guid recipeId);
        Task AddIngredientToRecipeAsync(Guid recipeId, RecipeIngredientDto ingredientDto);
        Task<IEnumerable<RecipeDto>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds);
        Task<IEnumerable<RecipeDto>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds);
        
        /// <summary>
        /// Generate AI recipes based on user preferences and save to database
        /// </summary>
        Task<List<RecipeDto>> GenerateAndSaveAIRecipeAsync(Guid userId, string cuisineType = "Vietnamese", int count = 3);
    }
}