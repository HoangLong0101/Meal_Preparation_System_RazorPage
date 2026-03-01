using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IAIOperationLogger
    {
        Task LogOperationAsync(AIOperationLog log);
        Task<AIOperationLog> StartOperationAsync(string operationType, string inputParameters, Guid? customerId = null);
        Task CompleteOperationAsync(int logId, string status, string outputSummary, int durationMs);
        Task FailOperationAsync(int logId, Exception ex, int durationMs);
    }
}
