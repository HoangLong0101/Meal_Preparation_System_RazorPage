namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class MenuMealDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
        public bool IsSoldOut { get; set; } // Calculated field when AvailableQuantity is 0
        public RecipeDto Recipe { get; set; } = new RecipeDto(); // Include recipe details for display
    }
}