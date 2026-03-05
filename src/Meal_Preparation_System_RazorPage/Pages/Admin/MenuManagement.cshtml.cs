using Meal_Preparation_System_RazorPage.Hubs;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Meal_Preparation_System_RazorPage.Pages.Admin
{
    public class MenuManagementModel : PageModel
    {
        private readonly IMenuService _menuService;
        private readonly IRecipeService _recipeService;
        private readonly IHubContext<MealPrepHub> _hubContext;

        public MenuManagementModel(IMenuService menuService, IRecipeService recipeService, IHubContext<MealPrepHub> hubContext)
        {
            _menuService = menuService;
            _recipeService = recipeService;
            _hubContext = hubContext;
        }

        public DailyMenuDto? TodayMenu { get; set; }
        public IEnumerable<DailyMenuDto> AllMenus { get; set; } = [];
        public IEnumerable<RecipeDto> AvailableRecipes { get; set; } = [];

        [BindProperty]
        public DateTime NewMenuDate { get; set; } = DateTime.Today;

        [BindProperty]
        public Guid AddToMenuId { get; set; }

        [BindProperty]
        public Guid SelectedRecipeId { get; set; }

        [BindProperty]
        public decimal MealPrice { get; set; }

        [BindProperty]
        public int MealQuantity { get; set; } = 20;

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateMenuAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                await _menuService.CreateDailyMenuAsync(NewMenuDate);
                TempData["SuccessMessage"] = $"Menu created for {NewMenuDate:MMM dd, yyyy}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddMealAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                var menuMealDto = new MenuMealDto
                {
                    RecipeId = SelectedRecipeId,
                    Price = MealPrice,
                    AvailableQuantity = MealQuantity
                };

                await _menuService.AddMealToMenuAsync(AddToMenuId, menuMealDto);

                var recipe = await _recipeService.GetByIdAsync(SelectedRecipeId);
                await _hubContext.Clients.Group("menu")
                    .SendAsync("MenuUpdated", "MealAdded", recipe.RecipeName, "");

                TempData["SuccessMessage"] = "Meal added to menu!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPublishAsync(Guid menuId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                await _menuService.PublishMenuAsync(menuId);

                await _hubContext.Clients.Group("menu")
                    .SendAsync("MenuUpdated", "Published", "", "");

                TempData["SuccessMessage"] = "Menu published!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(Guid menuMealId, int newQuantity)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                await _menuService.UpdateMealQuantityAsync(menuMealId, newQuantity);

                var meal = await _menuService.GetMenuMealAsync(menuMealId);
                if (meal != null)
                {
                    await _hubContext.Clients.Group("menu")
                        .SendAsync("MenuMealQuantityChanged", meal.RecipeName, meal.AvailableQuantity, meal.IsSoldOut);
                }

                TempData["SuccessMessage"] = "Quantity updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeactivateAsync(Guid menuId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                await _menuService.DeactivateMenuAsync(menuId);

                await _hubContext.Clients.Group("menu")
                    .SendAsync("MenuUpdated", "Deactivated", "", "");

                TempData["SuccessMessage"] = "Menu deactivated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReactivateAsync(Guid menuId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                await _menuService.ReactivateMenuAsync(menuId);

                await _hubContext.Clients.Group("menu")
                    .SendAsync("MenuUpdated", "Reactivated", "", "");

                TempData["SuccessMessage"] = "Menu reactivated!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            TodayMenu = await _menuService.GetByDateAsync(DateTime.Today);
            AllMenus = await _menuService.GetAllMenusAsync();
            AvailableRecipes = await _recipeService.GetAllAsync();
        }
    }
}
