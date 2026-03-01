using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for Account entity
    /// </summary>
    public class AccountRepository : Repository<Account>, IAccountRepository
    {
        public AccountRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet
                .AnyAsync(a => a.Email == email);
        }

        public async Task<Account?> GetWithHealthProfileAsync(Guid accountId)
        {
            return await _dbSet
                .Include(a => a.HealthProfile)
                    .ThenInclude(hp => hp.Allergies)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }
    }
}