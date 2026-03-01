namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for creating a new ingredient
    /// </summary>
    public class CreateIngredientDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public bool IsAllergen { get; set; }
    }
}