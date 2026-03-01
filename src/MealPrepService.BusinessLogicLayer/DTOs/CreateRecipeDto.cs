namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for creating a new recipe
    /// </summary>
    public class CreateRecipeDto
    {
        public string RecipeName { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public float TotalCalories { get; set; }
        public float ProteinG { get; set; }
        public float FatG { get; set; }
        public float CarbsG { get; set; }
        
        /// <summary>
        /// Ingredients for AI-generated recipes (ingredient name, quantity, unit)
        /// </summary>
        public List<CreateRecipeIngredientDto> Ingredients { get; set; } = new();
    }

    /// <summary>
    /// DTO for creating a recipe ingredient
    /// </summary>
    public class CreateRecipeIngredientDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}