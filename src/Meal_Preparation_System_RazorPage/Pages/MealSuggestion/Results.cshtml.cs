using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealSuggestion
{
    public class ResultsModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;
        private readonly IRecipeService _recipeService;
        private readonly ILogger<ResultsModel> _logger;

        public ResultsModel(
            IMealPlanService mealPlanService,
            IRecipeService recipeService,
            ILogger<ResultsModel> logger)
        {
            _mealPlanService = mealPlanService;
            _recipeService = recipeService;
            _logger = logger;
        }

        public List<SuggestionResultItem> Suggestions { get; set; } = new();
        public string OverallReasoning { get; set; } = string.Empty;
        public string MealType { get; set; } = string.Empty;
        public DateTime ServeDate { get; set; } = DateTime.Today;

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToPage("/Account/Login");

            var resultsJson = TempData["SuggestionResults"] as string;
            if (string.IsNullOrEmpty(resultsJson))
                return RedirectToPage("Index");

            try
            {
                var aiResponse = System.Text.Json.JsonSerializer.Deserialize<DailyMealSuggestionResponse>(resultsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (aiResponse == null)
                    return RedirectToPage("Index");

                MealType = TempData["SuggestionMealType"] as string ?? "Lunch";
                var serveDateStr = TempData["SuggestionServeDate"] as string;
                ServeDate = DateTime.TryParse(serveDateStr, out var sd) ? sd : DateTime.Today;
                OverallReasoning = aiResponse.Reasoning;

                Suggestions = aiResponse.Suggestions.Select(s => new SuggestionResultItem
                {
                    RecipeId = s.RecipeId,
                    RecipeName = s.MealName,
                    Description = s.Description,
                    EstimatedCalories = s.EstimatedCalories,
                    EstimatedPrice = s.EstimatedPrice,
                    PrepTime = s.PrepTime,
                    Reasoning = s.Reasoning,
                    IsFromDatabase = s.IsFromDatabase,
                    Instructions = s.Instructions ?? string.Empty,
                    ProteinG = s.ProteinG,
                    CarbsG = s.CarbsG,
                    FatG = s.FatG,
                    Ingredients = s.Ingredients?.Select(i => new SuggestionIngredientItem
                    {
                        Name = i.Name,
                        Quantity = i.Quantity,
                        Unit = i.Unit
                    }).ToList() ?? new List<SuggestionIngredientItem>(),
                    PerPersonPortions = s.PerPersonPortions?.Select(p => new SuggestionPersonPortionItem
                    {
                        Person = p.Person,
                        Portions = p.Portions,
                        QuantityGuide = p.QuantityGuide,
                        EstimatedCalories = p.EstimatedCalories,
                        Note = p.Note
                    }).ToList() ?? new List<SuggestionPersonPortionItem>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing suggestion results");
                return RedirectToPage("Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddToMealPlanAsync(
            string mealName,
            string mealType,
            string? serveDate,
            Guid? recipeId,
            string? ingredientsJson,
            string? perPersonPortionsJson,
            string? instructions,
            float calories,
            float proteinG,
            float carbsG,
            float fatG)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var accountId = Guid.Parse(accountIdStr);
                var targetDate = DateTime.TryParse(serveDate, out var sd) ? sd : DateTime.Today;

                _logger.LogInformation("AddToMealPlan called - MealName: {MealName}, MealType: {MealType}, ServeDate: {ServeDate}, RecipeId: {RecipeId}",
                    mealName, mealType, targetDate, recipeId);

                if (string.IsNullOrWhiteSpace(mealName))
                {
                    TempData["ErrorMessage"] = "Meal name is required.";
                    return RedirectToPage("Index");
                }

                if (string.IsNullOrWhiteSpace(mealType))
                    mealType = "Lunch";

                // Get or create active meal plan
                var activePlan = await _mealPlanService.GetActivePlanAsync(accountId);
                if (activePlan == null)
                {
                    var newPlanDto = new MealPlanDto
                    {
                        AccountId = accountId,
                        PlanName = $"Weekly Plan {DateTime.Now:MM/dd/yyyy}",
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(7),
                        IsAiGenerated = true
                    };
                    activePlan = await _mealPlanService.CreateManualMealPlanAsync(newPlanDto);
                    await _mealPlanService.SetActivePlanAsync(activePlan.Id, accountId);
                }

                var englishMealType = mealType.ToLower().Trim() switch
                {
                    "breakfast" => "breakfast",
                    "lunch" => "lunch",
                    "dinner" => "dinner",
                    "snack" => "snack",
                    _ => "lunch"
                };

                var mealDto = new MealDto
                {
                    PlanId = activePlan.Id,
                    MealType = englishMealType,
                    ServeDate = targetDate,
                    MealFinished = false,
                    Recipes = new List<RecipeDto>()
                };

                RecipeDto? recipeToAdd = null;
                string portionSummaryText = string.Empty;

                if (!string.IsNullOrWhiteSpace(perPersonPortionsJson))
                {
                    try
                    {
                        var portions = System.Text.Json.JsonSerializer.Deserialize<List<PerPersonPortionJsonHelper>>(perPersonPortionsJson,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (portions?.Any() == true)
                        {
                            var lines = portions.Select(p =>
                            {
                                var person = string.IsNullOrWhiteSpace(p.Person) ? "Member" : p.Person;
                                var portionsCount = p.Portions > 0 ? p.Portions : 1;
                                var quantityGuide = string.IsNullOrWhiteSpace(p.QuantityGuide) ? "N/A" : p.QuantityGuide;
                                var caloriesText = p.EstimatedCalories > 0 ? $" (~{p.EstimatedCalories:0} cal)" : string.Empty;
                                var noteText = string.IsNullOrWhiteSpace(p.Note) ? string.Empty : $" - {p.Note}";
                                return $"- {person}: {portionsCount} portion(s), {quantityGuide}{caloriesText}{noteText}";
                            });

                            portionSummaryText = "\n\nPer-person portions:\n" + string.Join("\n", lines);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse perPersonPortionsJson payload");
                    }
                }

                if (recipeId.HasValue && recipeId.Value != Guid.Empty)
                {
                    try
                    {
                        recipeToAdd = await _recipeService.GetByIdAsync(recipeId.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Recipe not found in database, will create new one");
                        recipeToAdd = null;
                    }
                }

                if (recipeToAdd == null)
                {
                    var createRecipeDto = new CreateRecipeDto
                    {
                        RecipeName = mealName.Trim(),
                        Instructions = (!string.IsNullOrWhiteSpace(instructions) ? instructions.Trim() : "AI-generated meal. Instructions will be updated.") + portionSummaryText,
                        TotalCalories = calories > 0 ? calories : 500,
                        ProteinG = proteinG >= 0 ? proteinG : 25,
                        FatG = fatG >= 0 ? fatG : 15,
                        CarbsG = carbsG >= 0 ? carbsG : 50,
                        Ingredients = new List<CreateRecipeIngredientDto>()
                    };

                    if (!string.IsNullOrWhiteSpace(ingredientsJson))
                    {
                        try
                        {
                            var ingredients = System.Text.Json.JsonSerializer.Deserialize<List<IngredientJsonHelper>>(ingredientsJson,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (ingredients != null)
                            {
                                foreach (var ing in ingredients)
                                {
                                    createRecipeDto.Ingredients.Add(new CreateRecipeIngredientDto
                                    {
                                        IngredientName = ing.Name ?? string.Empty,
                                        Quantity = ing.Quantity ?? 0,
                                        Unit = ing.Unit ?? "unit"
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse ingredients JSON");
                        }
                    }

                    recipeToAdd = await _recipeService.CreateRecipeAsync(createRecipeDto);
                }

                mealDto.Recipes.Add(recipeToAdd);
                await _mealPlanService.AddMealToPlanAsync(activePlan.Id, mealDto);

                TempData["SuccessMessage"] = $"Added '{mealName}' to your meal plan!";
                return RedirectToPage("/MealPlans/Details", new { id = activePlan.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding suggestion to meal plan");
                TempData["ErrorMessage"] = $"Unable to add meal to plan: {ex.Message}";
                return RedirectToPage("Index");
            }
        }
    }

    public class SuggestionResultItem
    {
        public Guid? RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float EstimatedCalories { get; set; }
        public float EstimatedPrice { get; set; }
        public int PrepTime { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public bool IsFromDatabase { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public float ProteinG { get; set; }
        public float CarbsG { get; set; }
        public float FatG { get; set; }
        public List<SuggestionIngredientItem> Ingredients { get; set; } = new();
        public List<SuggestionPersonPortionItem> PerPersonPortions { get; set; } = new();
    }

    public class SuggestionIngredientItem
    {
        public string Name { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class SuggestionPersonPortionItem
    {
        public string Person { get; set; } = string.Empty;
        public int Portions { get; set; } = 1;
        public string QuantityGuide { get; set; } = string.Empty;
        public float EstimatedCalories { get; set; }
        public string? Note { get; set; }
    }

    internal class IngredientJsonHelper
    {
        public string? Name { get; set; }
        public float? Quantity { get; set; }
        public string? Unit { get; set; }
    }

    internal class PerPersonPortionJsonHelper
    {
        public string? Person { get; set; }
        public int Portions { get; set; }
        public string? QuantityGuide { get; set; }
        public float EstimatedCalories { get; set; }
        public string? Note { get; set; }
    }
}
