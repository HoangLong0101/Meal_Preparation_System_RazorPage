using Meal_Preparation_System_RazorPage.Hubs;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Meal_Preparation_System_RazorPage.Pages.Admin
{
    public class OrdersModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IHubContext<MealPrepHub> _hubContext;

        public OrdersModel(IOrderService orderService, IHubContext<MealPrepHub> hubContext)
        {
            _orderService = orderService;
            _hubContext = hubContext;
        }

        public IEnumerable<OrderDto> Orders { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            Orders = await _orderService.GetAllOrdersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(Guid orderId, string newStatus)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role is not ("Admin" or "Manager"))
                return RedirectToPage("/Account/Login");

            try
            {
                await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

                var order = await _orderService.GetByIdAsync(orderId);
                await _hubContext.Clients.Group($"user-{order.AccountId}")
                    .SendAsync("OrderStatusUpdated", orderId.ToString(), newStatus);
                await _hubContext.Clients.Group($"order-{orderId}")
                    .SendAsync("OrderStatusUpdated", orderId.ToString(), newStatus);

                TempData["SuccessMessage"] = $"Order status updated to '{newStatus}'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
