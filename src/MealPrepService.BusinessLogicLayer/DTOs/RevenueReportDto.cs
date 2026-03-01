namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for revenue report data
    /// </summary>
    public class RevenueReportDto
    {
        public Guid Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalSubscriptionRevenue { get; set; }
        public decimal TotalOrderRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalRevenue => TotalSubscriptionRevenue + TotalOrderRevenue;
    }
}