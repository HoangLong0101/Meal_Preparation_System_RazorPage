using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for DailyMenu entity
    /// </summary>
    public class DailyMenuRepository : Repository<DailyMenu>, IDailyMenuRepository
    {
        public DailyMenuRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<DailyMenu?> GetByDateAsync(DateTime date)
        {
            var dateOnly = date.Date;
            return await _dbSet
                .Include(dm => dm.MenuMeals)
                    .ThenInclude(mm => mm.Recipe)
                .FirstOrDefaultAsync(dm => dm.MenuDate.Date == dateOnly);
        }

        public async Task<IEnumerable<DailyMenu>> GetWeeklyMenuAsync(DateTime startDate)
        {
            var endDate = startDate.AddDays(7);
            return await _dbSet
                .Include(dm => dm.MenuMeals)
                    .ThenInclude(mm => mm.Recipe)
                .Where(dm => dm.MenuDate >= startDate 
                    && dm.MenuDate < endDate 
                    && dm.Status == "active")
                .OrderBy(dm => dm.MenuDate)
                .ToListAsync();
        }

        public async Task<DailyMenu?> GetWithMealsAsync(Guid menuId)
        {
            return await _dbSet
                .Include(dm => dm.MenuMeals)
                    .ThenInclude(mm => mm.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(dm => dm.Id == menuId);
        }
    }
}