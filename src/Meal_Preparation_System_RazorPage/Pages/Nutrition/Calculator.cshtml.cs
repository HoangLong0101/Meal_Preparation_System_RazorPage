using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Nutrition
{
    public class CalculatorModel : PageModel
    {
        private readonly INutritionService _nutritionService;

        public CalculatorModel(INutritionService nutritionService)
        {
            _nutritionService = nutritionService;
        }

        [BindProperty]
        public List<IngredientInputModel> Ingredients { get; set; } = new();

        public List<IngredientResultModel> IngredientResults { get; set; } = new();

        public float TotalCalories { get; set; }
        public float TotalProteinG { get; set; }
        public float TotalCarbsG { get; set; }
        public float TotalFatG { get; set; }
        public string Advice { get; set; } = "";
        public string MealRating { get; set; } = "";
        public List<SuggestionModel> Suggestions { get; set; } = new();

        public bool IsCalculated { get; set; }

        public void OnGet()
        {
            Ingredients.Add(new IngredientInputModel());
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var result = await _nutritionService.CalculateAsync(
                    Ingredients.Select(i =>
                        $"{i.Amount}{i.Unit} {i.IngredientName}").ToList());

                TotalCalories = result.TotalCalories;
                TotalProteinG = result.TotalProteinG;
                TotalCarbsG = result.TotalCarbsG;
                TotalFatG = result.TotalFatG;
                Advice = result.Advice;
                MealRating = result.MealRating;

                Suggestions = result.Suggestions
                    .Select(s => new SuggestionModel
                    {
                        Ingredient = s.Ingredient,
                        Amount = s.Amount,
                        Reason = s.Reason,
                        Category = s.Category
                    }).ToList();

                IngredientResults = result.Ingredients
                    .Select(r => new IngredientResultModel
                    {
                        IngredientName = r.Name,
                        Amount = r.Amount,
                        Unit = r.Unit,
                        Calories = r.Calories,
                        ProteinG = r.ProteinG,
                        CarbsG = r.CarbsG,
                        FatG = r.FatG
                    }).ToList();

                IsCalculated = true;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return Page();
        }
    }

    public class IngredientInputModel
    {
        public string IngredientName { get; set; } = "";
        public float Amount { get; set; }
        public string Unit { get; set; } = "";
    }

    public class IngredientResultModel
    {
        public string IngredientName { get; set; } = "";
        public float Amount { get; set; }
        public string Unit { get; set; }
        public float Calories { get; set; }
        public float ProteinG { get; set; }
        public float CarbsG { get; set; }
        public float FatG { get; set; }
    }

    public class SuggestionModel
    {
        public string Ingredient { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Category { get; set; } = "";
    }
}
