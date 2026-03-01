namespace MealPrepService.DataAccessLayer.Entities;

public class HealthProfile : BaseEntity
{
    public Guid AccountId { get; set; }
    public int Age { get; set; }
    public float Weight { get; set; }
    public float Height { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? HealthNotes { get; set; }
    
    // AI Recommendation fields
    public string? DietaryRestrictions { get; set; }  // e.g., "vegetarian", "vegan", "keto", "low-carb", "high-protein"
    public string? FoodPreferences { get; set; }  // Free-text field for food preferences (e.g., "loves chicken, prefers spicy food, dislikes mushrooms")
    public int? CalorieGoal { get; set; }  // Daily calorie target
    
    // Navigation properties
    public Account Account { get; set; } = null!;
    public ICollection<Allergy> Allergies { get; set; } = new List<Allergy>();
}
