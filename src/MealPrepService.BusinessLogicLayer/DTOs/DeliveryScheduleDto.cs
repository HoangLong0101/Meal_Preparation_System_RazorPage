namespace MealPrepService.BusinessLogicLayer.DTOs;

public class DeliveryScheduleDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateTime DeliveryTime { get; set; }
    public string Address { get; set; } = string.Empty;
    public string DriverContact { get; set; } = string.Empty;
    public OrderDto? Order { get; set; }
}