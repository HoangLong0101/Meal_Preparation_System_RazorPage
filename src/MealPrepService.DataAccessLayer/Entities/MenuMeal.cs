namespace MealPrepService.DataAccessLayer.Entities;

public class MenuMeal : BaseEntity
{
    public Guid MenuId { get; set; }
    public Guid RecipeId { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
    
    // Navigation properties
    public DailyMenu Menu { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
