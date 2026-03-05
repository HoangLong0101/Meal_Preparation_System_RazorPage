using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly IHealthProfileService _healthProfileService;
        private readonly IAllergyService _allergyService;
        private readonly IFamilyMemberService _familyMemberService;

        public ProfileModel(
            IHealthProfileService healthProfileService,
            IAllergyService allergyService,
            IFamilyMemberService familyMemberService)
        {
            _healthProfileService = healthProfileService;
            _allergyService = allergyService;
            _familyMemberService = familyMemberService;
        }

        [BindProperty]
        public HealthProfileDto Profile { get; set; } = new();

        public IEnumerable<AllergyDto> AvailableAllergies { get; set; } = [];

        public bool IsNewProfile { get; set; }

        [BindProperty]
        public Guid? SelectedAllergyId { get; set; }

        public IEnumerable<FamilyMemberDto> FamilyMembers { get; set; } = [];

        [BindProperty]
        public string? NewMemberName { get; set; }

        [BindProperty]
        public int? NewMemberAge { get; set; }

        [BindProperty]
        public string? NewMemberNotes { get; set; }

        public float Bmi
        {
            get
            {
                var heightM = Profile.Height / 100f;
                return heightM > 0 ? Profile.Weight / (heightM * heightM) : 0;
            }
        }

        public string BmiCategory => Bmi switch
        {
            < 18.5f => "Underweight",
            < 25f => "Normal",
            < 30f => "Overweight",
            _ => "Obese"
        };

        public string BmiColor => Bmi switch
        {
            < 18.5f => "var(--clr-warning)",
            < 25f => "var(--clr-success)",
            < 30f => "var(--clr-warning)",
            _ => "var(--clr-danger)"
        };

        public async Task<IActionResult> OnGetAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var accountId = Guid.Parse(accountIdStr);
            AvailableAllergies = await _allergyService.GetAllAsync();
            FamilyMembers = await _familyMemberService.GetByAccountIdAsync(accountId);

            try
            {
                Profile = await _healthProfileService.GetByAccountIdAsync(accountId);
                IsNewProfile = false;
            }
            catch
            {
                Profile = new HealthProfileDto { AccountId = accountId };
                IsNewProfile = true;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                Profile.AccountId = Guid.Parse(accountIdStr);
                await _healthProfileService.CreateOrUpdateAsync(Profile);
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddAllergyAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            if (SelectedAllergyId == null || SelectedAllergyId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Please select an allergy.";
                return RedirectToPage();
            }

            try
            {
                var accountId = Guid.Parse(accountIdStr);
                var profile = await _healthProfileService.GetByAccountIdAsync(accountId);
                await _healthProfileService.AddAllergyAsync(profile.Id, SelectedAllergyId.Value);
                TempData["SuccessMessage"] = "Allergy added to your profile.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAllergyAsync(Guid allergyId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var accountId = Guid.Parse(accountIdStr);
                var profile = await _healthProfileService.GetByAccountIdAsync(accountId);
                await _healthProfileService.RemoveAllergyAsync(profile.Id, allergyId);
                TempData["SuccessMessage"] = "Allergy removed from your profile.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddMemberAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            if (string.IsNullOrWhiteSpace(NewMemberName))
            {
                TempData["ErrorMessage"] = "Member name is required.";
                return RedirectToPage();
            }

            try
            {
                await _familyMemberService.AddAsync(new FamilyMemberDto
                {
                    AccountId = Guid.Parse(accountIdStr),
                    Name = NewMemberName,
                    Age = NewMemberAge,
                    Notes = NewMemberNotes
                });
                TempData["SuccessMessage"] = $"{NewMemberName} added to your family.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync(Guid memberId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                await _familyMemberService.RemoveAsync(memberId, Guid.Parse(accountIdStr));
                TempData["SuccessMessage"] = "Family member removed.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
