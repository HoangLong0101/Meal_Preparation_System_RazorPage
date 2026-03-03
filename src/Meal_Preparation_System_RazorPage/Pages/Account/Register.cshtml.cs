using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IAccountService _accountService;

        public RegisterModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public CreateAccountDto Input { get; set; } = new();

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return Page();
            }

            try
            {
                await _accountService.RegisterAsync(Input);
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToPage("/Account/Login");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
        }
    }
}
