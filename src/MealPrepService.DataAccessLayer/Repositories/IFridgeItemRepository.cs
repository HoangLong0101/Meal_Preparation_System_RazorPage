using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for FridgeItem entity
    /// </summary>
    public interface IFridgeItemRepository : IRepository<FridgeItem>
    {
        Task<IEnumerable<FridgeItem>> GetByAccountIdAsync(Guid accountId);
        Task<IEnumerable<FridgeItem>> GetExpiringItemsAsync(Guid accountId, int daysThreshold);
    }
}