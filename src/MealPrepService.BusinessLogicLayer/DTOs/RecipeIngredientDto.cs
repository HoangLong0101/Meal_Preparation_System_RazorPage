namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for recipe ingredient relationship
    /// </summary>
    public class RecipeIngredientDto
    {
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public float Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public bool IsAllergen { get; set; }
    }
}