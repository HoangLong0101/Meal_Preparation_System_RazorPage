namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class RecipeDto
    {
        public Guid Id { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        
        // Nutrition fields - populated from existing recipe data
        public float TotalCalories { get; set; }
        public float ProteinG { get; set; }
        public float FatG { get; set; }
        public float CarbsG { get; set; }
        
        // Ingredients with amounts and units
        public List<RecipeIngredientDto> Ingredients { get; set; } = new List<RecipeIngredientDto>();
    }
}