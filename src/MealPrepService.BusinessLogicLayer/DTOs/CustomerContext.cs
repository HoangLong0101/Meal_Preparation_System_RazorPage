using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class CustomerContext
    {
        public Account Customer { get; set; } = null!;
        public HealthProfile? HealthProfile { get; set; }
        public List<Allergy> Allergies { get; set; } = new();
        public List<Order> OrderHistory { get; set; } = new();
        public bool HasCompleteProfile { get; set; }
        public List<string> MissingDataWarnings { get; set; } = new();
    }
}
