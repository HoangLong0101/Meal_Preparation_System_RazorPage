namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for updating an existing ingredient
    /// </summary>
    public class UpdateIngredientDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public bool IsAllergen { get; set; }
        public List<Guid>? AllergyIds { get; set; }
    }
}