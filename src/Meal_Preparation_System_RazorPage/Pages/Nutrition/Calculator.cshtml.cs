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

        public bool IsCalculated { get; set; }

        public void OnGet()
        {
            Ingredients.Add(new IngredientInputModel());
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Validate and format ingredients
                var validatedIngredients = Ingredients
                    .Where(i => !string.IsNullOrWhiteSpace(i.IngredientName) && i.Amount > 0)
                    .Select(i => FormatIngredientForAI(i))
                    .ToList();

                if (!validatedIngredients.Any())
                {
                    TempData["ErrorMessage"] = "Please enter at least one ingredient with a valid amount.";
                    return Page();
                }

                var result = await _nutritionService.CalculateAsync(validatedIngredients);

                TotalCalories = result.TotalCalories;
                TotalProteinG = result.TotalProteinG;
                TotalCarbsG = result.TotalCarbsG;
                TotalFatG = result.TotalFatG;
                Advice = result.Advice;

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
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = $"Calculation error: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            }

            return Page();
        }

        private static string FormatIngredientForAI(IngredientInputModel ingredient)
        {
            // Format: "250 g chicken breast" - clear and unambiguous for AI parsing
            var unit = string.IsNullOrWhiteSpace(ingredient.Unit) ? "g" : ingredient.Unit.Trim();
            return $"{ingredient.Amount} {unit} {ingredient.IngredientName.Trim()}";
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
}
