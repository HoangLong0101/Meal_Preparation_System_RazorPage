using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class MealRecommendation
    {
        public Recipe Recipe { get; set; } = null!;
        public double RelevanceScore { get; set; }
        public string ReasoningExplanation { get; set; } = string.Empty;
        public NutritionalInfo NutritionalInfo { get; set; } = null!;
        
        // Properties for meal plan generation
        public DateTime Date { get; set; }
        public string MealType { get; set; } = string.Empty; // breakfast, lunch, dinner
        public List<Guid> RecommendedRecipeIds { get; set; } = new();
    }

    public class NutritionalInfo
    {
        public decimal TotalCalories { get; set; }
        public decimal ProteinG { get; set; }
        public decimal FatG { get; set; }
        public decimal CarbsG { get; set; }
    }

    public class NutritionalSummary
    {
        public decimal TotalCalories { get; set; }
        public decimal TotalProteinG { get; set; }
        public decimal TotalFatG { get; set; }
        public decimal TotalCarbsG { get; set; }
        public decimal ProteinRatio { get; set; }
        public decimal CarbRatio { get; set; }
        public decimal FatRatio { get; set; }
    }

    public class RecommendationResult
    {
        public bool Success { get; set; }
        public List<MealRecommendation> Recommendations { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public NutritionalSummary? NutritionalSummary { get; set; }
    }
}
