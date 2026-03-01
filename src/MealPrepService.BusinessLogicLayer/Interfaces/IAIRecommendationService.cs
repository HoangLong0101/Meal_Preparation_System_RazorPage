using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IAIRecommendationService
    {
        Task<RecommendationResult> GenerateRecommendationsAsync(Guid customerId);
        Task<IEnumerable<MealRecommendation>> GetMealPlanRecommendationsAsync(Guid customerId, DateTime startDate, DateTime endDate);
        Task<bool> IsAIEnabledAsync();
    }
}
