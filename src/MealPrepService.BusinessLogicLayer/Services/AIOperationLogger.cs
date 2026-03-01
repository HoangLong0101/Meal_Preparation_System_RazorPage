using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class AIOperationLogger : IAIOperationLogger
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AIOperationLogger> _logger;

        public AIOperationLogger(
            IUnitOfWork unitOfWork,
            ILogger<AIOperationLogger> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogOperationAsync(AIOperationLog log)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            _unitOfWork.AIOperationLogs.Add(log);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<AIOperationLog> StartOperationAsync(string operationType, string inputParameters, Guid? customerId = null)
        {
            var log = new AIOperationLog
            {
                OperationType = operationType,
                Timestamp = DateTime.UtcNow,
                Status = "InProgress",
                CustomerId = customerId,
                InputParameters = inputParameters,
                ExecutionDurationMs = 0
            };

            _unitOfWork.AIOperationLogs.Add(log);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("AI operation started: {OperationType} (LogId: {LogId})", operationType, log.Id);

            return log;
        }

        public async Task CompleteOperationAsync(int logId, string status, string outputSummary, int durationMs)
        {
            var log = await _unitOfWork.AIOperationLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("Cannot complete operation: Log {LogId} not found", logId);
                return;
            }

            log.Status = status;
            log.OutputSummary = outputSummary;
            log.ExecutionDurationMs = durationMs;

            _unitOfWork.AIOperationLogs.Update(log);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("AI operation completed: {OperationType} (LogId: {LogId}, Status: {Status}, Duration: {Duration}ms)", 
                log.OperationType, logId, status, durationMs);
        }

        public async Task FailOperationAsync(int logId, Exception ex, int durationMs)
        {
            var log = await _unitOfWork.AIOperationLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("Cannot fail operation: Log {LogId} not found", logId);
                return;
            }

            log.Status = "Failure";
            log.ErrorMessage = ex.Message;
            log.StackTrace = ex.StackTrace;
            log.ExecutionDurationMs = durationMs;

            _unitOfWork.AIOperationLogs.Update(log);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogError(ex, "AI operation failed: {OperationType} (LogId: {LogId}, Duration: {Duration}ms)", 
                log.OperationType, logId, durationMs);
        }
    }
}
