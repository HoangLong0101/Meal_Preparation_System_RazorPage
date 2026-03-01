namespace MealPrepService.DataAccessLayer.Entities;

public class SubscriptionPackage : BaseEntity
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string? Description { get; set; }
    
    // Navigation properties
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
