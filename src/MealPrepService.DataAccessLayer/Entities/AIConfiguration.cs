namespace MealPrepService.DataAccessLayer.Entities
{
    public class AIConfiguration : BaseEntity
    {
        public bool IsEnabled { get; set; } = true;
        public int MinRecommendations { get; set; } = 5;
        public int MaxRecommendations { get; set; } = 10;
        public int RecommendationCacheDurationMinutes { get; set; } = 60;
        public string? ConfigurationJson { get; set; }
        public new DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
