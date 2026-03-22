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
            var targetDayOfWeek = date.DayOfWeek;

            var menus = await _dbSet
                .Include(dm => dm.MenuMeals)
                    .ThenInclude(mm => mm.Recipe)
                .ToListAsync();

            return menus
                .Where(dm => dm.MenuDate.DayOfWeek == targetDayOfWeek)
                .OrderBy(dm => dm.Status == "active" ? 0 : dm.Status == "draft" ? 1 : 2)
                .ThenByDescending(dm => dm.UpdatedAt ?? dm.CreatedAt)
                .FirstOrDefault();
        }

        public async Task<IEnumerable<DailyMenu>> GetWeeklyMenuAsync(DateTime startDate)
        {
            var menus = await _dbSet
                .Include(dm => dm.MenuMeals)
                    .ThenInclude(mm => mm.Recipe)
                .Where(dm => dm.Status == "active")
                .ToListAsync();

            return menus
                .OrderBy(dm => (int)dm.MenuDate.DayOfWeek)
                .ThenBy(dm => dm.MenuDate)
                .ToList();
        }

        public async Task<IEnumerable<DailyMenu>> GetAllMenusAsync()
        {
            var menus = await _dbSet
                .Include(dm => dm.MenuMeals)
                    .ThenInclude(mm => mm.Recipe)
                .ToListAsync();

            return menus
                .OrderBy(dm => (int)dm.MenuDate.DayOfWeek)
                .ThenByDescending(dm => dm.CreatedAt)
                .ToList();
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