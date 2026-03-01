using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class AIRecommendationService : IAIRecommendationService
    {
        private readonly IAIConfigurationService _configService;
        private readonly ICustomerProfileAnalyzer _profileAnalyzer;
        private readonly IRecommendationEngine _recommendationEngine;
        private readonly IAIOperationLogger _operationLogger;
        private readonly ILogger<AIRecommendationService> _logger;

        public AIRecommendationService(
            IAIConfigurationService configService,
            ICustomerProfileAnalyzer profileAnalyzer,
            IRecommendationEngine recommendationEngine,
            IAIOperationLogger operationLogger,
            ILogger<AIRecommendationService> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _profileAnalyzer = profileAnalyzer ?? throw new ArgumentNullException(nameof(profileAnalyzer));
            _recommendationEngine = recommendationEngine ?? throw new ArgumentNullException(nameof(recommendationEngine));
            _operationLogger = operationLogger ?? throw new ArgumentNullException(nameof(operationLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsAIEnabledAsync()
        {
            var config = await _configService.GetConfigurationAsync();
            return config.IsEnabled;
        }

        public async Task<RecommendationResult> GenerateRecommendationsAsync(Guid customerId)
        {
            var stopwatch = Stopwatch.StartNew();
            var inputParams = JsonSerializer.Serialize(new { customerId });
            
            // Start operation logging
            var operationLog = await _operationLogger.StartOperationAsync(
                "Recommendation", 
                inputParams, 
                customerId);

            try
            {
                // Check if AI is enabled
                if (!await IsAIEnabledAsync())
                {
                    var disabledResult = new RecommendationResult
                    {
                        Success = false,
                        ErrorMessage = "AI recommendation feature is currently disabled. Please contact support."
                    };

                    stopwatch.Stop();
                    await _operationLogger.CompleteOperationAsync(
                        operationLog.Id, 
                        "Warning", 
                        "AI disabled", 
                        (int)stopwatch.ElapsedMilliseconds);

                    return disabledResult;
                }

                // Get AI configuration
                var config = await _configService.GetConfigurationAsync();

                // Analyze customer profile
                var customerContext = await _profileAnalyzer.AnalyzeCustomerAsync(customerId);

                // Generate recommendations
                var recommendations = await _recommendationEngine.GenerateRecommendationsAsync(
                    customerContext,
                    config.MinRecommendations,
                    config.MaxRecommendations);

                // Calculate nutritional summary
                var nutritionalSummary = CalculateNutritionalSummary(recommendations);

                var result = new RecommendationResult
                {
                    Success = true,
                    Recommendations = recommendations,
                    NutritionalSummary = nutritionalSummary
                };

                stopwatch.Stop();
                var outputSummary = JsonSerializer.Serialize(new 
                { 
                    recommendationCount = recommendations.Count,
                    hasCompleteProfile = customerContext.HasCompleteProfile,
                    warnings = customerContext.MissingDataWarnings
                });

                await _operationLogger.CompleteOperationAsync(
                    operationLog.Id,
                    "Success",
                    outputSummary,
                    (int)stopwatch.ElapsedMilliseconds);

                _logger.LogInformation("Successfully generated {Count} recommendations for customer {CustomerId} in {Duration}ms",
                    recommendations.Count, customerId, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _operationLogger.FailOperationAsync(
                    operationLog.Id,
                    ex,
                    (int)stopwatch.ElapsedMilliseconds);

                _logger.LogError(ex, "Failed to generate recommendations for customer {CustomerId}", customerId);

                return new RecommendationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while generating recommendations. Please try again later."
                };
            }
        }

        public async Task<IEnumerable<MealRecommendation>> GetMealPlanRecommendationsAsync(Guid customerId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Generating AI meal plan recommendations for customer {CustomerId} from {StartDate} to {EndDate}", 
                customerId, startDate, endDate);

            try
            {
                // Check if AI is enabled
                if (!await IsAIEnabledAsync())
                {
                    _logger.LogWarning("AI is disabled, returning empty recommendations");
                    return Enumerable.Empty<MealRecommendation>();
                }

                // Get AI configuration
                var config = await _configService.GetConfigurationAsync();

                // Analyze customer profile
                var customerContext = await _profileAnalyzer.AnalyzeCustomerAsync(customerId);

                // Generate recommendations for each day and meal type
                var recommendations = new List<MealRecommendation>();
                var mealTypes = new[] { "Breakfast", "Lunch", "Dinner" };
                var currentDate = startDate;

                while (currentDate <= endDate)
                {
                    foreach (var mealType in mealTypes)
                    {
                        // Generate diversity-aware recommendations for this specific meal
                        var mealRecommendations = await _recommendationEngine.GenerateRecommendationsAsync(
                            customerContext,
                            1, // Min 1 recipe per meal
                            1, // Max 1 recipe per meal for better variety
                            currentDate,
                            mealType
                        );

                        if (mealRecommendations.Any())
                        {
                            // Use the first (and likely only) recommendation
                            var mealRecommendation = mealRecommendations.First();
                            
                            // Ensure the recommendation has the correct date and meal type
                            mealRecommendation.Date = currentDate;
                            mealRecommendation.MealType = mealType;

                            recommendations.Add(mealRecommendation);

                            _logger.LogDebug("Generated {MealType} recommendation for {Date}: {RecipeName}", 
                                mealType, currentDate, mealRecommendation.Recipe.RecipeName);
                        }
                        else
                        {
                            _logger.LogWarning("No recommendations generated for {MealType} on {Date}", mealType, currentDate);
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                _logger.LogInformation("Generated {Count} meal recommendations for customer {CustomerId}", 
                    recommendations.Count, customerId);

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate meal plan recommendations for customer {CustomerId}", customerId);
                return Enumerable.Empty<MealRecommendation>();
            }
        }

        private NutritionalSummary CalculateNutritionalSummary(List<MealRecommendation> recommendations)
        {
            if (!recommendations.Any())
            {
                return new NutritionalSummary();
            }

            var totalCalories = recommendations.Sum(r => r.NutritionalInfo.TotalCalories);
            var totalProtein = recommendations.Sum(r => r.NutritionalInfo.ProteinG);
            var totalFat = recommendations.Sum(r => r.NutritionalInfo.FatG);
            var totalCarbs = recommendations.Sum(r => r.NutritionalInfo.CarbsG);

            var summary = new NutritionalSummary
            {
                TotalCalories = totalCalories,
                TotalProteinG = totalProtein,
                TotalFatG = totalFat,
                TotalCarbsG = totalCarbs
            };

            // Calculate ratios (protein and carbs = 4 cal/g, fat = 9 cal/g)
            if (totalCalories > 0)
            {
                summary.ProteinRatio = (totalProtein * 4) / totalCalories;
                summary.CarbRatio = (totalCarbs * 4) / totalCalories;
                summary.FatRatio = (totalFat * 9) / totalCalories;
            }

            return summary;
        }
    }
}
