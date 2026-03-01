using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for Recipe entity
    /// </summary>
    public interface IRecipeRepository : IRepository<Recipe>
    {
        Task<IEnumerable<Recipe>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds);
        Task<IEnumerable<Recipe>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds);
        Task<IEnumerable<Recipe>> GetAllWithIngredientsAsync();
        Task<bool> IsUsedInActiveMenuAsync(Guid recipeId);
    }
}