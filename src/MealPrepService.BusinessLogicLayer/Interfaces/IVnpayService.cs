using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IVnpayService
    {
        Task<VnpayPaymentUrlDto> CreatePaymentUrlAsync(Guid orderId, decimal amount, string orderInfo);
        Task<VnpayCallbackResult> ProcessCallbackAsync(VnpayCallbackDto callbackDto);
        bool ValidateCallback(VnpayCallbackDto callbackDto);
    }
}