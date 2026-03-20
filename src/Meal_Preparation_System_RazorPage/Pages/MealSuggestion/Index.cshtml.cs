using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Meal_Preparation_System_RazorPage.Pages.MealSuggestion
{
    public class IndexModel : PageModel
    {
        private readonly ILLMService _llmService;
        private readonly IHealthProfileService _healthProfileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ILLMService llmService,
            IHealthProfileService healthProfileService,
            IUnitOfWork unitOfWork,
            ILogger<IndexModel> logger)
        {
            _llmService = llmService;
            _healthProfileService = healthProfileService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please select a meal type")]
        public string MealType { get; set; } = "Lunch";

        [BindProperty]
        [Required(ErrorMessage = "Please select a preparation method")]
        public string PreparationMethod { get; set; } = "Cooking";

        [BindProperty]
        public string? CurrentMood { get; set; }

        [BindProperty]
        [Range(0, 10000000, ErrorMessage = "Budget must be between 0 and 10,000,000 VND")]
        public int? Budget { get; set; }

        [BindProperty]
        [Range(0, 480, ErrorMessage = "Time must be between 0 and 480 minutes")]
        public int? AvailableTime { get; set; }

        [BindProperty]
        [StringLength(500, ErrorMessage = "Specific request cannot exceed 500 characters")]
        public string? SpecificRequest { get; set; }

        [BindProperty]
        public bool PrioritizeHealthy { get; set; } = false;

        [BindProperty]
        [Range(1, 20, ErrorMessage = "Number of people must be between 1 and 20")]
        public int NumberOfPeople { get; set; } = 1;

        [BindProperty]
        public List<FamilyMemberInfo> FamilyMembers { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Please select a serve date")]
        [DataType(DataType.Date)]
        public DateTime ServeDate { get; set; } = DateTime.Today;

        public static readonly List<string> MealTypeOptions = new()
        {
            "Breakfast", "Lunch", "Dinner", "Snack"
        };

        public static readonly List<string> PreparationMethodOptions = new()
        {
            "Cooking", "Order"
        };

        public static readonly List<string> MoodOptions = new()
        {
            "Happy", "Sad", "Stressed", "Tired", "Excited", "Normal"
        };

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToPage("/Account/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
                return Page();

            try
            {
                var accountId = Guid.Parse(accountIdStr);

                // Get health profile
                HealthProfileDto healthProfile;
                try
                {
                    healthProfile = await _healthProfileService.GetByAccountIdAsync(accountId);
                }
                catch (BusinessException)
                {
                    TempData["ErrorMessage"] = "Please create a health profile before using Meal Suggestions.";
                    return RedirectToPage("/Index");
                }

                if (healthProfile == null)
                {
                    TempData["ErrorMessage"] = "Please create a health profile before using this feature.";
                    return RedirectToPage("/Index");
                }

                // Get account entity
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Account information not found.";
                    return RedirectToPage("/Index");
                }

                // Get health profile entity with allergies
                var healthProfileEntities = await _unitOfWork.HealthProfiles.FindAsync(hp => hp.AccountId == accountId);
                var healthProfileEntity = healthProfileEntities.FirstOrDefault();

                // Get allergies as entities
                var allergyEntities = healthProfile.Allergies?
                    .Select(allergyName => new Allergy
                    {
                        Id = Guid.NewGuid(),
                        AllergyName = allergyName
                    })
                    .ToList() ?? new List<Allergy>();

                // Build customer context
                var context = new CustomerContext
                {
                    Customer = account,
                    HealthProfile = healthProfileEntity,
                    Allergies = allergyEntities,
                    HasCompleteProfile = healthProfileEntity != null
                };

                // Build request
                var validFamilyMembers = FamilyMembers?
                    .Where(fm => !string.IsNullOrWhiteSpace(fm.Role))
                    .Select(fm => new FamilyMemberInfo
                    {
                        Role = fm.Role,
                        Note = fm.Note,
                        Portion = fm.Portion > 0 ? fm.Portion : 1
                    })
                    .ToList() ?? new List<FamilyMemberInfo>();

                var request = new DailyMealRequest
                {
                    MealType = MealType,
                    PreparationMethod = PreparationMethod,
                    CurrentMood = CurrentMood,
                    Budget = Budget,
                    AvailableTime = AvailableTime,
                    SpecificRequest = SpecificRequest,
                    PrioritizeHealthy = PrioritizeHealthy,
                    NumberOfPeople = NumberOfPeople,
                    FamilyMembers = validFamilyMembers
                };

                // Get available recipes from database
                var recipes = await _unitOfWork.Recipes.GetAllAsync();
                var recipeList = recipes.ToList();

                // Generate suggestions using AI
                var aiResponse = await _llmService.GenerateDailyMealSuggestionsAsync(context, request, recipeList);

                // Store results in TempData for the Results page
                TempData["SuggestionResults"] = System.Text.Json.JsonSerializer.Serialize(aiResponse);
                TempData["SuggestionMealType"] = MealType;
                TempData["SuggestionServeDate"] = ServeDate.ToString("yyyy-MM-dd");
                TempData["SuggestionPreparationMethod"] = PreparationMethod;

                return RedirectToPage("Results");
            }
            catch (BusinessException bex)
            {
                _logger.LogWarning(bex, "Business rule violation during meal suggestion generation");
                ModelState.AddModelError(string.Empty, bex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily meal suggestions: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "Unable to generate meal suggestions. Please try again.");
                return Page();
            }
        }
    }
}
