namespace MealPrepService.DataAccessLayer.Entities;

public class MealPlan : BaseEntity
{
    public Guid AccountId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAiGenerated { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public Account Account { get; set; } = null!;
    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
}
