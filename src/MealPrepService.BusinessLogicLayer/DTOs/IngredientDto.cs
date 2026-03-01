namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for ingredient data
    /// </summary>
    public class IngredientDto
    {
        public Guid Id { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public bool IsAllergen { get; set; }
        public List<AllergyDto> Allergies { get; set; } = new List<AllergyDto>();
    }
}