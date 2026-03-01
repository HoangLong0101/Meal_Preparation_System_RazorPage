namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class UpdateAccountDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Password { get; set; } // Optional - only update if provided
        public string Role { get; set; } = string.Empty;
    }
}
