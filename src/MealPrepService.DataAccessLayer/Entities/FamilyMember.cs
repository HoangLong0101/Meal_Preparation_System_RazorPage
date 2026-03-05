namespace MealPrepService.DataAccessLayer.Entities;

public class FamilyMember : BaseEntity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Account Account { get; set; } = null!;
}
