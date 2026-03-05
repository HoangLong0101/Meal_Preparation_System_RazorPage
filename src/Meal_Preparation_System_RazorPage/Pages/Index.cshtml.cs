using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ILLMService _llmService;
        private readonly ICustomerProfileAnalyzer _profileAnalyzer;
        private readonly IRecipeService _recipeService;
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(
            ILogger<IndexModel> logger,
            ILLMService llmService,
            ICustomerProfileAnalyzer profileAnalyzer,
            IRecipeService recipeService,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _llmService = llmService;
            _profileAnalyzer = profileAnalyzer;
            _recipeService = recipeService;
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public string MealType { get; set; } = "Lunch";

        [BindProperty]
        public string? MealPreference { get; set; }

        public DailyMealSuggestionResponse? SuggestionResult { get; set; }
        public bool HasGenerated { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostGenerateMealAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);

            try
            {
                var customerContext = await _profileAnalyzer.AnalyzeCustomerAsync(accountId);
                var allRecipes = (await _unitOfWork.Recipes.GetAllAsync()).ToList();

                var request = new DailyMealRequest
                {
                    MealType = MealType,
                    SpecificRequest = MealPreference,
                    PrioritizeHealthy = true
                };

                SuggestionResult = await _llmService.GenerateDailyMealSuggestionsAsync(
                    customerContext, request, allRecipes);

                HasGenerated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI meal generation failed on home page");
                TempData["ErrorMessage"] = $"AI generation failed: {ex.Message}";
            }

            return Page();
        }
    }
}
