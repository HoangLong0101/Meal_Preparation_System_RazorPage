namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class DailyMenuDto
    {
        public Guid Id { get; set; }
        public DateTime MenuDate { get; set; }
        public string Status { get; set; } = string.Empty; // draft, active
        public List<MenuMealDto> MenuMeals { get; set; } = new List<MenuMealDto>();
    }
}