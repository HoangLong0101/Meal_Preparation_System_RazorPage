namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for updating an existing recipe
    /// </summary>
    public class UpdateRecipeDto
    {
        public string RecipeName { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }
}