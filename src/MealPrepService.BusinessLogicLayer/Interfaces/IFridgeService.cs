using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IFridgeService
    {
        Task<IEnumerable<FridgeItemDto>> GetFridgeItemsAsync(Guid accountId);
        Task<FridgeItemDto> AddItemAsync(FridgeItemDto dto);
        Task UpdateItemQuantityAsync(Guid itemId, float newQuantity);
        Task RemoveItemAsync(Guid itemId);
        Task<IEnumerable<FridgeItemDto>> GetExpiringItemsAsync(Guid accountId);
        Task<GroceryListDto> GenerateGroceryListAsync(Guid accountId, Guid planId);
        Task<GroceryListDto> GenerateGroceryListFromActivePlanAsync(Guid accountId);
    }
}