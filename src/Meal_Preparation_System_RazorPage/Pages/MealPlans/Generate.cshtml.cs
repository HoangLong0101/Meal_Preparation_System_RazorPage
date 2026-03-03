using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.MealPlans
{
    public class GenerateModel : PageModel
    {
        private readonly IMealPlanService _mealPlanService;
        private readonly IHealthProfileService _healthProfileService;
        private readonly IFamilyMemberService _familyMemberService;

        public GenerateModel(
            IMealPlanService mealPlanService,
            IHealthProfileService healthProfileService,
            IFamilyMemberService familyMemberService)
        {
            _mealPlanService = mealPlanService;
            _healthProfileService = healthProfileService;
            _familyMemberService = familyMemberService;
        }

        [BindProperty]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [BindProperty]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(6);

        [BindProperty]
        public string? PlanName { get; set; }

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int Servings { get; set; } = 1;

        public HealthProfileDto? UserProfile { get; set; }
        public bool HasProfile { get; set; }
        public IEnumerable<FamilyMemberDto> FamilyMembers { get; set; } = [];
        public int MaxFamilySize { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AccountId") == null)
                return RedirectToPage("/Account/Login");

            await LoadProfileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var plan = await _mealPlanService.GenerateAiMealPlanAsync(
                    Guid.Parse(accountIdStr), StartDate, EndDate, PlanName);
                TempData["SuccessMessage"] = "AI Meal Plan generated successfully!";
                return RedirectToPage("/MealPlans/Details", new { id = plan.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                await LoadProfileAsync();
                return Page();
            }
        }

        private async Task LoadProfileAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null) return;

            var accountId = Guid.Parse(accountIdStr);
            FamilyMembers = await _familyMemberService.GetByAccountIdAsync(accountId);
            MaxFamilySize = FamilyMembers.Count() + 1; // +1 for user
            if (MaxFamilySize < 1) MaxFamilySize = 1;
            Servings = MaxFamilySize;

            try
            {
                UserProfile = await _healthProfileService.GetByAccountIdAsync(accountId);
                HasProfile = true;
            }
            catch
            {
                HasProfile = false;
            }
        }
    }
}
