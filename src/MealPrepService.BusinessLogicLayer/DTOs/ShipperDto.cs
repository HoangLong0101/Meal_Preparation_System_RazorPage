namespace MealPrepService.BusinessLogicLayer.DTOs;

public class ShipperDto
{
    public Guid Id { get; set; }
    public Guid? AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
