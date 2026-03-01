namespace MealPrepService.DataAccessLayer.Entities;

public class MealRecipe
{
    public Guid MealId { get; set; }
    public Guid RecipeId { get; set; }
    
    // Navigation properties
    public Meal Meal { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
