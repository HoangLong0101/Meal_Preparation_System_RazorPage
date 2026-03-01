using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IRecommendationEngine
    {
        Task<List<MealRecommendation>> GenerateRecommendationsAsync(
            CustomerContext context, 
            int minCount, 
            int maxCount);

        Task<List<MealRecommendation>> GenerateRecommendationsAsync(
            CustomerContext context,
            int minCount,
            int maxCount,
            DateTime targetDate,
            string mealType);
    }
}
