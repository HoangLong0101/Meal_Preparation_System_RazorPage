using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for Account entity
    /// </summary>
    public interface IAccountRepository : IRepository<Account>
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<Account?> GetWithHealthProfileAsync(Guid accountId);
    }
}