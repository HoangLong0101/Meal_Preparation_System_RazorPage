namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class HealthProfileDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public int Age { get; set; }
        public float Weight { get; set; }
        public float Height { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string HealthNotes { get; set; } = string.Empty;
        public string? DietaryRestrictions { get; set; }
        public string? FoodPreferences { get; set; }
        public int? CalorieGoal { get; set; }
        public List<string> Allergies { get; set; } = new List<string>();
    }
}