using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IMenuService
    {
        Task<DailyMenuDto> CreateDailyMenuAsync(DateTime menuDate);
        Task<DailyMenuDto?> GetByDateAsync(DateTime date);
        Task<IEnumerable<DailyMenuDto>> GetWeeklyMenuAsync(DateTime startDate);
        Task AddMealToMenuAsync(Guid menuId, MenuMealDto menuMealDto);
        Task PublishMenuAsync(Guid menuId);
        Task DeactivateMenuAsync(Guid menuId);
        Task ReactivateMenuAsync(Guid menuId);
        Task<IEnumerable<DailyMenuDto>> GetAllMenusAsync();
        Task UpdateMealQuantityAsync(Guid menuMealId, int newQuantity);
        Task<MenuMealDto?> GetMenuMealAsync(Guid menuMealId);
    }
}