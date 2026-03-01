using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for MealPlan entity
    /// </summary>
    public interface IMealPlanRepository : IRepository<MealPlan>
    {
        Task<IEnumerable<MealPlan>> GetByAccountIdAsync(Guid accountId);
        Task<MealPlan?> GetWithMealsAndRecipesAsync(Guid planId);
        Task<List<Guid>> GetRecentRecipeIdsAsync(Guid accountId, DateTime fromDate, DateTime toDate);
    }
}