using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for MealPlan entity
    /// </summary>
    public class MealPlanRepository : Repository<MealPlan>, IMealPlanRepository
    {
        public MealPlanRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MealPlan>> GetByAccountIdAsync(Guid accountId)
        {
            return await _dbSet
                .Where(mp => mp.AccountId == accountId)
                .OrderByDescending(mp => mp.StartDate)
                .ToListAsync();
        }

        public async Task<MealPlan?> GetWithMealsAndRecipesAsync(Guid planId)
        {
            var mealPlan = await _dbSet
                .Include(mp => mp.Meals)
                    .ThenInclude(m => m.MealRecipes)
                        .ThenInclude(mr => mr.Recipe)
                            .ThenInclude(r => r.RecipeIngredients)
                                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(mp => mp.Id == planId);

            // Order meals by date and meal type after loading
            if (mealPlan?.Meals != null)
            {
                mealPlan.Meals = mealPlan.Meals
                    .OrderBy(m => m.ServeDate)
                    .ThenBy(m => GetMealTypeOrder(m.MealType))
                    .ToList();
            }

            return mealPlan;
        }

        private static int GetMealTypeOrder(string mealType)
        {
            return mealType?.ToLower() switch
            {
                "breakfast" => 1,
                "lunch" => 2,
                "dinner" => 3,
                _ => 4
            };
        }

        public async Task<List<Guid>> GetRecentRecipeIdsAsync(Guid accountId, DateTime fromDate, DateTime toDate)
        {
            return await _dbSet
                .Where(mp => mp.AccountId == accountId)
                .Where(mp => mp.StartDate <= toDate && mp.EndDate >= fromDate)
                .SelectMany(mp => mp.Meals)
                .Where(m => m.ServeDate >= fromDate && m.ServeDate <= toDate)
                .SelectMany(m => m.MealRecipes)
                .Select(mr => mr.RecipeId)
                .Distinct()
                .ToListAsync();
        }
    }
}