using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for Order entity
    /// </summary>
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetByAccountIdAsync(Guid accountId);
        Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Order?> GetWithDetailsAsync(Guid orderId);
        Task<decimal> GetTotalRevenueByMonthAsync(int year, int month);
    }
}