using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Service interface for ingredient management operations
    /// </summary>
    public interface IIngredientService
    {
        Task<IngredientDto> CreateIngredientAsync(CreateIngredientDto dto);
        Task<IngredientDto> GetByIdAsync(Guid ingredientId);
        Task<IEnumerable<IngredientDto>> GetAllAsync();
        Task<IngredientDto> UpdateIngredientAsync(Guid ingredientId, UpdateIngredientDto dto);
        Task DeleteIngredientAsync(Guid ingredientId);
        Task<IEnumerable<IngredientDto>> GetAllergensAsync();
    }
}