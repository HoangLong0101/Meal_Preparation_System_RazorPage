using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface IDeliveryService
{
    Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(Guid orderId, DeliveryScheduleDto dto);
    Task<IEnumerable<DeliveryScheduleDto>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<DeliveryScheduleDto>> GetByDeliveryManAsync(Guid deliveryManId);
    Task CompleteDeliveryAsync(Guid deliveryId);
    Task UpdateDeliveryTimeAsync(Guid deliveryId, DateTime newTime);
}