using Meal_Preparation_System_RazorPage.Hubs;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Meal_Preparation_System_RazorPage.Pages.Orders
{
    public class CreateModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;
        private readonly IHubContext<MealPrepHub> _hubContext;

        public CreateModel(IOrderService orderService, IMenuService menuService, IHubContext<MealPrepHub> hubContext)
        {
            _orderService = orderService;
            _menuService = menuService;
            _hubContext = hubContext;
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

                // Skip payment — auto-confirm the order
                await _orderService.UpdateOrderStatusAsync(order.Id, "confirmed");
                var confirmedOrder = await _orderService.GetByIdAsync(order.Id);

                // Notify all menu viewers about the quantity change
                var updatedMeal = await _menuService.GetMenuMealAsync(MenuMealId);
                if (updatedMeal != null)
                {
                    var updateType = updatedMeal.IsSoldOut ? "SoldOut" : "QuantityChanged";
                    var detail = updatedMeal.IsSoldOut ? "0" : $"{updatedMeal.AvailableQuantity}";
                    await _hubContext.Clients.Group("menu")
                        .SendAsync("MenuMealQuantityChanged", updatedMeal.RecipeName, updatedMeal.AvailableQuantity, updatedMeal.IsSoldOut);
                }

                // Notify admin that a new order was placed
                await _hubContext.Clients.Group("admin")
                    .SendAsync("NewOrderPlaced", confirmedOrder.Id.ToString(),
                        confirmedOrder.OrderDate.ToString("MMM dd, yyyy HH:mm"),
                        confirmedOrder.TotalAmount.ToString("N0"),
                        confirmedOrder.Status,
                        confirmedOrder.OrderDetails.Count);

                TempData["SuccessMessage"] = "Order placed and confirmed!";
                return RedirectToPage("/Orders/Details", new { id = confirmedOrder.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Menu/Index");
            }
        }
    }
}
