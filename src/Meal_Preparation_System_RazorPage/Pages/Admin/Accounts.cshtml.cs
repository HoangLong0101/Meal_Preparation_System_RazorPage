using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Admin
{
    public class AccountsModel : PageModel
    {
        private readonly IAccountService _accountService;

        public AccountsModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public IEnumerable<AccountDto> StaffAccounts { get; set; } = [];

        [BindProperty]
        public string NewEmail { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string NewFullName { get; set; } = string.Empty;

        [BindProperty]
        public string NewRole { get; set; } = "Manager";

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToPage("/Account/Login");

            StaffAccounts = await _accountService.GetAllStaffAccountsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToPage("/Account/Login");

            try
            {
                var createDto = new CreateAccountDto
                {
                    Email = NewEmail,
                    Password = NewPassword,
                    FullName = NewFullName
                };

                await _accountService.CreateStaffAccountAsync(createDto, NewRole);
                TempData["SuccessMessage"] = $"Account '{NewFullName}' created as {NewRole}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid accountId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToPage("/Account/Login");

            try
            {
                await _accountService.DeleteStaffAccountAsync(accountId);
                TempData["SuccessMessage"] = "Account deleted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
