namespace MealPrepService.DataAccessLayer.Entities;

public class UserSubscription : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid PackageId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty; // active, expired
    
    // Navigation properties
    public Account Account { get; set; } = null!;
    public SubscriptionPackage Package { get; set; } = null!;
}
