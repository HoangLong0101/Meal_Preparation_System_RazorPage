namespace MealPrepService.DataAccessLayer.Entities;

public class FridgeItem : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid IngredientId { get; set; }
    public float CurrentAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    
    // Navigation properties
    public Account Account { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
}
