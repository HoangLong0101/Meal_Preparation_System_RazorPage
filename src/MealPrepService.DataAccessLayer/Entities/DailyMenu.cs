namespace MealPrepService.DataAccessLayer.Entities;

public class DailyMenu : BaseEntity
{
    public DateTime MenuDate { get; set; }
    public string Status { get; set; } = string.Empty; // draft, active
    
    // Navigation properties
    public ICollection<MenuMeal> MenuMeals { get; set; } = new List<MenuMeal>();
}
