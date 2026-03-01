namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class MealPlanDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAiGenerated { get; set; }
        public bool IsActive { get; set; }
        public List<MealDto> Meals { get; set; } = new List<MealDto>();
    }
}