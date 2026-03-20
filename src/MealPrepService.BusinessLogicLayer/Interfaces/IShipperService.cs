using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface IShipperService
{
    Task<IEnumerable<ShipperDto>> GetActiveShippersAsync();
}
