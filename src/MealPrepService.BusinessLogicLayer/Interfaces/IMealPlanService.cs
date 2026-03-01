using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IMealPlanService
    {
        Task<MealPlanDto> GenerateAiMealPlanAsync(Guid accountId, DateTime startDate, DateTime endDate);
        Task<MealPlanDto> GenerateAiMealPlanAsync(Guid accountId, DateTime startDate, DateTime endDate, string? customPlanName);
        Task<MealPlanDto> CreateManualMealPlanAsync(MealPlanDto dto);
        Task<MealPlanDto?> GetByIdAsync(Guid planId);
        Task<IEnumerable<MealPlanDto>> GetByAccountIdAsync(Guid accountId);
        Task<MealPlanDto?> GetActivePlanAsync(Guid accountId);
        Task AddMealToPlanAsync(Guid planId, MealDto mealDto);
        Task DeleteAsync(Guid planId, Guid requestingAccountId);
        Task SetActivePlanAsync(Guid planId, Guid accountId);
        Task RemoveRecipeFromMealAsync(Guid mealId, Guid recipeId, Guid accountId);
        Task MarkMealAsFinishedAsync(Guid mealId, Guid accountId, bool finished);
    }
}