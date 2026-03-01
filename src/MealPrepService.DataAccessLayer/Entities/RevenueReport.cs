namespace MealPrepService.DataAccessLayer.Entities;

public class RevenueReport : BaseEntity
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalSubscriptionRev { get; set; }
    public decimal TotalOrderRev { get; set; }
    public int TotalOrdersCount { get; set; }
}
