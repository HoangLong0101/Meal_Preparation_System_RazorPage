using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for UserSubscription entity
    /// </summary>
    public interface IUserSubscriptionRepository : IRepository<UserSubscription>
    {
        Task<UserSubscription?> GetActiveSubscriptionAsync(Guid accountId);
        Task<IEnumerable<UserSubscription>> GetExpiredSubscriptionsAsync();
        Task<int> GetActiveSubscriptionCountAsync();
    }
}