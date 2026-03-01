using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for Recipe entity
    /// </summary>
    public class RecipeRepository : Repository<Recipe>, IRecipeRepository
    {
        public RecipeRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Recipe>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds)
        {
            return await _dbSet
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Where(r => r.RecipeIngredients.Any(ri => ingredientIds.Contains(ri.IngredientId)))
                .ToListAsync();
        }

        public async Task<IEnumerable<Recipe>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds)
        {
            return await _dbSet
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Where(r => !r.RecipeIngredients.Any(ri => 
                    ri.Ingredient.IsAllergen && allergyIds.Contains(ri.IngredientId)))
                .ToListAsync();
        }

        public async Task<IEnumerable<Recipe>> GetAllWithIngredientsAsync()
        {
            return await _dbSet
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();
        }

        public async Task<bool> IsUsedInActiveMenuAsync(Guid recipeId)
        {
            return await _context.MenuMeals
                .Include(mm => mm.Menu)
                .AnyAsync(mm => mm.RecipeId == recipeId 
                    && mm.Menu.Status == "active");
        }
    }
}