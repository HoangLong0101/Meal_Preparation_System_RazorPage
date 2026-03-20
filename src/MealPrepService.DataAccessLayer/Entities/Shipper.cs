namespace MealPrepService.DataAccessLayer.Entities;

public class Shipper : BaseEntity
{
    public Guid? AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Account? Account { get; set; }
    public ICollection<DeliverySchedule> DeliverySchedules { get; set; } = new List<DeliverySchedule>();
}
