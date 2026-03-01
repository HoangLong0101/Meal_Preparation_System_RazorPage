namespace MealPrepService.DataAccessLayer.Entities;

public class Ingredient : BaseEntity
{
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public float CaloPerUnit { get; set; }
    public bool IsAllergen { get; set; }
    
    // Navigation properties
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<FridgeItem> FridgeItems { get; set; } = new List<FridgeItem>();
    public ICollection<Allergy> Allergies { get; set; } = new List<Allergy>();
}
