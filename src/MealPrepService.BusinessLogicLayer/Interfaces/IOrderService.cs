using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid accountId, List<OrderItemDto> items);
    Task<OrderDto> ProcessPaymentAsync(Guid orderId, string paymentMethod);
    Task<OrderDto> ProcessVnpayCallbackAsync(VnpayCallbackDto callbackDto);
    Task<OrderDto> ConfirmCashPaymentAsync(Guid orderId, Guid deliveryManId);
    Task<OrderDto> GetByIdAsync(Guid orderId);
    Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId);
    Task UpdateOrderStatusAsync(Guid orderId, string status);
}