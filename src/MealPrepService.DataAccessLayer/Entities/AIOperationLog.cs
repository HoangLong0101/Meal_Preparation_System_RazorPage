namespace MealPrepService.DataAccessLayer.Entities
{
    public class AIOperationLog
    {
        public int Id { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
        public string? InputParameters { get; set; }
        public string? OutputSummary { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public int ExecutionDurationMs { get; set; }

        // Navigation property
        public Account? Customer { get; set; }
    }
}
