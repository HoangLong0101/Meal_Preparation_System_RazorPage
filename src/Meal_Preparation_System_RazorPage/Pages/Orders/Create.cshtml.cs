using Meal_Preparation_System_RazorPage.Hubs;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Meal_Preparation_System_RazorPage.Pages.Orders
{
    public class CreateModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;
        private readonly IVnpayService _vnpayService;
        private readonly IDeliveryService _deliveryService;
        private readonly IHubContext<MealPrepHub> _hubContext;
        private readonly string _mapboxAccessToken;

        public CreateModel(
            IOrderService orderService,
            IMenuService menuService,
            IVnpayService vnpayService,
            IDeliveryService deliveryService,
            IConfiguration configuration,
            IHubContext<MealPrepHub> hubContext)
        {
            _orderService = orderService;
            _menuService = menuService;
            _vnpayService = vnpayService;
            _deliveryService = deliveryService;
            _mapboxAccessToken = configuration["Mapbox:AccessToken"] ?? string.Empty;
            _hubContext = hubContext;
        }

        public MenuMealDto? SelectedMeal { get; set; }
        public string MapboxAccessToken => _mapboxAccessToken;

        [BindProperty]
        public Guid MenuMealId { get; set; }

        [BindProperty]
        public int Quantity { get; set; } = 1;

        [BindProperty]
        public string PaymentMethod { get; set; } = "COD";

        [BindProperty]
        public string DeliveryAddress { get; set; } = string.Empty;

        [BindProperty]
        public DateTime DeliveryTime { get; set; }

        [BindProperty]
        public string DriverContact { get; set; } = "TBD";

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
            DeliveryTime = DateTime.Today.AddDays(1).AddHours(11);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                if (string.IsNullOrWhiteSpace(DeliveryAddress))
                {
                    throw new InvalidOperationException("Delivery address is required.");
                }

                var items = new List<OrderItemDto>
                {
                    new() { MenuMealId = MenuMealId, Quantity = Quantity }
                };

                var order = await _orderService.CreateOrderAsync(Guid.Parse(accountIdStr), items);
                var normalizedPaymentMethod = PaymentMethod.Equals("VNPAY", StringComparison.OrdinalIgnoreCase)
                    ? "VNPAY"
                    : "COD";

                await _orderService.ProcessPaymentAsync(order.Id, normalizedPaymentMethod);

                var deliveryDto = new DeliveryScheduleDto
                {
                    OrderId = order.Id,
                    DeliveryTime = ToUtc(DeliveryTime),
                    Address = DeliveryAddress,
                    DriverContact = string.IsNullOrWhiteSpace(DriverContact) ? "TBD" : DriverContact
                };

                if (normalizedPaymentMethod == "COD")
                {
                    await _deliveryService.CreateDeliveryScheduleAsync(order.Id, deliveryDto);
                    var latestOrder = await _orderService.GetByIdAsync(order.Id);

                    await NotifyMenuMealChangedAsync(MenuMealId);
                    await NotifyOrderPlacedAsync(latestOrder);

                    TempData["SuccessMessage"] = "Order placed successfully. Please pay when receiving your meal.";
                    return RedirectToPage("/Orders/Details", new { id = latestOrder.Id });
                }

                var pendingDelivery = new PendingDeliveryInfo
                {
                    Address = deliveryDto.Address,
                    DeliveryTimeUtc = deliveryDto.DeliveryTime,
                    DriverContact = deliveryDto.DriverContact
                };

                HttpContext.Session.SetString(
                    GetPendingDeliveryKey(order.Id),
                    JsonSerializer.Serialize(pendingDelivery));

                var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(
                    order.Id,
                    order.TotalAmount,
                    $"Thanh_toan_don_hang_{order.Id:N}");

                return Redirect(paymentUrl.PaymentUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Menu/Index");
            }
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            if (value.Kind == DateTimeKind.Unspecified)
                value = DateTime.SpecifyKind(value, DateTimeKind.Local);

            return value.ToUniversalTime();
        }

        private static string GetPendingDeliveryKey(Guid orderId) => $"pending_delivery_{orderId}";

        private async Task NotifyMenuMealChangedAsync(Guid menuMealId)
        {
            var updatedMeal = await _menuService.GetMenuMealAsync(menuMealId);
            if (updatedMeal != null)
            {
                await _hubContext.Clients.Group("menu")
                    .SendAsync("MenuMealQuantityChanged", updatedMeal.RecipeName, updatedMeal.AvailableQuantity, updatedMeal.IsSoldOut);
            }
        }

        private async Task NotifyOrderPlacedAsync(OrderDto order)
        {
            await _hubContext.Clients.Group("admin")
                .SendAsync("NewOrderPlaced",
                    order.Id.ToString(),
                    order.OrderDate.ToString("MMM dd, yyyy HH:mm"),
                    order.TotalAmount.ToString("N0"),
                    order.Status,
                    order.OrderDetails.Count);
        }

        private sealed class PendingDeliveryInfo
        {
            public string Address { get; set; } = string.Empty;
            public DateTime DeliveryTimeUtc { get; set; }
            public string DriverContact { get; set; } = string.Empty;
        }
    }
}
