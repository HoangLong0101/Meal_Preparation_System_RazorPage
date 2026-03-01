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
        public decimal CurrentMonthRevenue { get; set; }
    }
}