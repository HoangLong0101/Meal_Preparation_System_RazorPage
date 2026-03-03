using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Orders
{
    public class CreateModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;

        public CreateModel(IOrderService orderService, IMenuService menuService)
        {
            _orderService = orderService;
            _menuService = menuService;
        }

        public MenuMealDto? SelectedMeal { get; set; }

        [BindProperty]
        public Guid MenuMealId { get; set; }

        [BindProperty]
        public int Quantity { get; set; } = 1;

        [BindProperty]
        public string PaymentMethod { get; set; } = "Cash";

        public async Task<IActionResult> OnGetAsync(Guid menuMealId)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            var todayMenu = await _menuService.GetByDateAsync(DateTime.Today);
            SelectedMeal = todayMenu?.MenuMeals.FirstOrDefault(m => m.Id == menuMealId);

            if (SelectedMeal == null)
            {
                TempData["ErrorMessage"] = "Menu item not found.";
                return RedirectToPage("/Menu/Index");
            }

            MenuMealId = menuMealId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                var items = new List<OrderItemDto>
                {
                    new() { MenuMealId = MenuMealId, Quantity = Quantity }
                };

                var order = await _orderService.CreateOrderAsync(Guid.Parse(accountIdStr), items);
                var processedOrder = await _orderService.ProcessPaymentAsync(order.Id, PaymentMethod);

                TempData["SuccessMessage"] = "Order placed successfully!";
                return RedirectToPage("/Orders/Details", new { id = processedOrder.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Menu/Index");
            }
        }
    }
}
