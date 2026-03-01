namespace MealPrepService.DataAccessLayer.Entities;

public class DeliverySchedule : BaseEntity
{
    public Guid OrderId { get; set; }
    public DateTime DeliveryTime { get; set; }
    public string Address { get; set; } = string.Empty;
    public string DriverContact { get; set; } = string.Empty;
    
    // Navigation properties
    public Order Order { get; set; } = null!;
}
