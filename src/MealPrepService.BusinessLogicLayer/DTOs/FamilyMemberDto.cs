namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class FamilyMemberDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Notes { get; set; }
    }
}
