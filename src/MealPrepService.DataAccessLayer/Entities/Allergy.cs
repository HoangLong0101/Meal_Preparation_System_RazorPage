namespace MealPrepService.DataAccessLayer.Entities;

public class Allergy : BaseEntity
{
    public string AllergyName { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<HealthProfile> HealthProfiles { get; set; } = new List<HealthProfile>();
    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}
