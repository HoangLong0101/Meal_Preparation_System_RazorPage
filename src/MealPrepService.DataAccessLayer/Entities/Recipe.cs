namespace MealPrepService.DataAccessLayer.Entities;

public class Recipe : BaseEntity
{
    public string RecipeName { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public float TotalCalories { get; set; }
    public float ProteinG { get; set; }
    public float FatG { get; set; }
    public float CarbsG { get; set; }
    
    // Navigation properties
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<MealRecipe> MealRecipes { get; set; } = new List<MealRecipe>();
    public ICollection<MenuMeal> MenuMeals { get; set; } = new List<MenuMeal>();
}
