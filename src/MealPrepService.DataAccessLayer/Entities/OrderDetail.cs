namespace MealPrepService.DataAccessLayer.Entities;

public class OrderDetail : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid MenuMealId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    // Navigation properties
    public Order Order { get; set; } = null!;
    public MenuMeal MenuMeal { get; set; } = null!;
}
