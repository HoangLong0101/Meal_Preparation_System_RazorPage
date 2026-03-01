namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class MealDto
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public string MealType { get; set; } = string.Empty; // breakfast, lunch, dinner
        public DateTime ServeDate { get; set; }
        public bool MealFinished { get; set; } = false;
        public List<RecipeDto> Recipes { get; set; } = new List<RecipeDto>();
    }
}
