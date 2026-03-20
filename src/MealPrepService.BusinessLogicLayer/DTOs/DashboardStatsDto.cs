namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for admin dashboard statistics
    /// </summary>
    public class DashboardStatsDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int PendingOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
        public decimal CurrentYearRevenue { get; set; }
    }

    public class DishRevenueDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueTrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}