using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for DailyMenu entity
    /// </summary>
    public interface IDailyMenuRepository : IRepository<DailyMenu>
    {
        Task<DailyMenu?> GetByDateAsync(DateTime date);
        Task<IEnumerable<DailyMenu>> GetWeeklyMenuAsync(DateTime startDate);
        Task<IEnumerable<DailyMenu>> GetAllMenusAsync();
        Task<DailyMenu?> GetWithMealsAsync(Guid menuId);
    }
}