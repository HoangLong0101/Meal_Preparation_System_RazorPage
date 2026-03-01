using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    public static class RecipeIngredientExtensions
    {
        public static async Task<List<RecipeIngredient>> GetByRecipeIdAsync(
            this DbSet<RecipeIngredient> recipeIngredients, 
            Guid recipeId)
        {
            return await recipeIngredients
                .Where(ri => ri.RecipeId == recipeId)
                .ToListAsync();
        }
    }
}
