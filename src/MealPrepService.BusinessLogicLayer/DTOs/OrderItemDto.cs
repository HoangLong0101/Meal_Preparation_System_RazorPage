namespace MealPrepService.BusinessLogicLayer.DTOs;

public class OrderItemDto
{
    public Guid MenuMealId { get; set; }
    public int Quantity { get; set; }
}