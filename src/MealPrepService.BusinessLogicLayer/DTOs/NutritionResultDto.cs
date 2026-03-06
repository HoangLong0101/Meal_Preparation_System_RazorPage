using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class NutritionResultDto
    {
        public float TotalCalories { get; set; }
        public float TotalProteinG { get; set; }
        public float TotalCarbsG { get; set; }
        public float TotalFatG { get; set; }

        public List<IngredientNutritionDto> Ingredients { get; set; } = new();
        public string Advice { get; set; } = string.Empty;
        public string MealRating { get; set; } = string.Empty;
        public List<MealImprovementDto> Suggestions { get; set; } = new();
    }

    public class MealImprovementDto
    {
        public string Ingredient { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class IngredientNutritionDto
    {
        public string Name { get; set; } = string.Empty;
        public float Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
        public float Calories { get; set; }
        public float ProteinG { get; set; }
        public float CarbsG { get; set; }
        public float FatG { get; set; }
    }
}
