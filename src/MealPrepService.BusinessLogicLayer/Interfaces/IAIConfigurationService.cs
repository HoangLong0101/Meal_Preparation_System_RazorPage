using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IAIConfigurationService
    {
        Task<AIConfiguration> GetConfigurationAsync();
        Task<AIConfiguration> UpdateConfigurationAsync(AIConfiguration config, string adminUsername);
        Task<IEnumerable<AIOperationLog>> GetOperationLogsAsync(int pageNumber, int pageSize, string? filterStatus = null);
    }
}
