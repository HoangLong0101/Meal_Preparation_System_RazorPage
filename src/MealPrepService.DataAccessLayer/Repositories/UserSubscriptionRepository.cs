using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for UserSubscription entity
    /// </summary>
    public class UserSubscriptionRepository : Repository<UserSubscription>, IUserSubscriptionRepository
    {
        public UserSubscriptionRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid accountId)
        {
            return await _dbSet
                .Include(us => us.Package)
                .Where(us => us.AccountId == accountId 
                    && us.Status == "active" 
                    && us.EndDate >= DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<UserSubscription>> GetExpiredSubscriptionsAsync()
        {
            return await _dbSet
                .Where(us => us.Status == "active" && us.EndDate < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<int> GetActiveSubscriptionCountAsync()
        {
            return await _dbSet
                .CountAsync(us => us.Status == "active" && us.EndDate >= DateTime.UtcNow);
        }
    }
}