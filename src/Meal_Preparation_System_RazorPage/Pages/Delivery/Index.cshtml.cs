using Meal_Preparation_System_RazorPage.Hubs;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace Meal_Preparation_System_RazorPage.Pages.Delivery
{
    public class IndexModel : PageModel
    {
        private readonly IDeliveryService _deliveryService;
        private readonly IOrderService _orderService;
        private readonly IHubContext<MealPrepHub> _hubContext;

        public IndexModel(
            IDeliveryService deliveryService,
            IOrderService orderService,
            IHubContext<MealPrepHub> hubContext)
        {
            _deliveryService = deliveryService;
            _orderService = orderService;
            _hubContext = hubContext;
        }

        public List<DeliveryScheduleDto> Deliveries { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var sessionGuard = GetDeliveryManContext();
            if (sessionGuard.Result != null)
            {
                return sessionGuard.Result;
            }

            Deliveries = (await _deliveryService.GetByDeliveryManAsync(sessionGuard.AccountId)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmCashPaymentAsync(Guid orderId)
        {
            var sessionGuard = GetDeliveryManContext();
            if (sessionGuard.Result != null)
            {
                return sessionGuard.Result;
            }

            try
            {
                var order = await _orderService.ConfirmCashPaymentAsync(orderId, sessionGuard.AccountId);
                await NotifyOrderStatusUpdatedAsync(order.Id, order.AccountId, order.Status);
                TempData["SuccessMessage"] = "COD payment confirmed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRescheduleAsync(Guid deliveryId, DateTime newDeliveryTime)
        {
            var sessionGuard = GetDeliveryManContext();
            if (sessionGuard.Result != null)
            {
                return sessionGuard.Result;
            }

            try
            {
                await _deliveryService.UpdateDeliveryTimeAsync(deliveryId, ToUtc(newDeliveryTime));
                TempData["SuccessMessage"] = "Delivery time updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteDeliveryAsync(Guid deliveryId, Guid orderId)
        {
            var sessionGuard = GetDeliveryManContext();
            if (sessionGuard.Result != null)
            {
                return sessionGuard.Result;
            }

            try
            {
                await _deliveryService.CompleteDeliveryAsync(deliveryId);
                var order = await _orderService.GetByIdAsync(orderId);
                await NotifyOrderStatusUpdatedAsync(order.Id, order.AccountId, order.Status);
                TempData["SuccessMessage"] = "Delivery completed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        private async Task NotifyOrderStatusUpdatedAsync(Guid orderId, Guid accountId, string status)
        {
            await _hubContext.Clients.Group($"user-{accountId}")
                .SendAsync("OrderStatusUpdated", orderId.ToString(), status);
            await _hubContext.Clients.Group($"order-{orderId}")
                .SendAsync("OrderStatusUpdated", orderId.ToString(), status);
            await _hubContext.Clients.Group("admin")
                .SendAsync("OrderStatusUpdated", orderId.ToString(), status);
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Local);
            }

            return value.ToUniversalTime();
        }

        private (Guid AccountId, IActionResult? Result) GetDeliveryManContext()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "DeliveryMan")
            {
                return (Guid.Empty, RedirectToPage("/Account/Login"));
            }

            var accountIdValue = HttpContext.Session.GetString("AccountId");
            if (!Guid.TryParse(accountIdValue, out var accountId))
            {
                return (Guid.Empty, RedirectToPage("/Account/Login"));
            }

            return (accountId, null);
        }
    }
}