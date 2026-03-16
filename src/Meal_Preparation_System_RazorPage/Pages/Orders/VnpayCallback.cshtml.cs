using Meal_Preparation_System_RazorPage.Hubs;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Meal_Preparation_System_RazorPage.Pages.Orders
{
    public class VnpayCallbackModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IDeliveryService _deliveryService;
        private readonly IHubContext<MealPrepHub> _hubContext;

        public VnpayCallbackModel(
            IOrderService orderService,
            IDeliveryService deliveryService,
            IHubContext<MealPrepHub> hubContext)
        {
            _orderService = orderService;
            _deliveryService = deliveryService;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var callbackDto = new VnpayCallbackDto
                {
                    vnp_TmnCode = Request.Query["vnp_TmnCode"].ToString(),
                    vnp_Amount = Request.Query["vnp_Amount"].ToString(),
                    vnp_BankCode = Request.Query["vnp_BankCode"].ToString(),
                    vnp_BankTranNo = Request.Query["vnp_BankTranNo"].ToString(),
                    vnp_CardType = Request.Query["vnp_CardType"].ToString(),
                    vnp_PayDate = Request.Query["vnp_PayDate"].ToString(),
                    vnp_OrderInfo = Request.Query["vnp_OrderInfo"].ToString(),
                    vnp_TransactionNo = Request.Query["vnp_TransactionNo"].ToString(),
                    vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString(),
                    vnp_TransactionStatus = Request.Query["vnp_TransactionStatus"].ToString(),
                    vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString(),
                    vnp_SecureHashType = Request.Query["vnp_SecureHashType"].ToString(),
                    vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString()
                };

                var order = await _orderService.ProcessVnpayCallbackAsync(callbackDto);

                if (order.Status == "confirmed")
                {
                    await CreatePendingDeliveryScheduleAsync(order.Id);

                    var latestOrder = await _orderService.GetByIdAsync(order.Id);
                    await _hubContext.Clients.Group("admin")
                        .SendAsync("NewOrderPlaced",
                            latestOrder.Id.ToString(),
                            latestOrder.OrderDate.ToString("MMM dd, yyyy HH:mm"),
                            latestOrder.TotalAmount.ToString("N0"),
                            latestOrder.Status,
                            latestOrder.OrderDetails.Count);

                    TempData["SuccessMessage"] = "VNPAY payment successful. Your order is confirmed.";
                    return RedirectToPage("/Orders/Details", new { id = order.Id });
                }

                TempData["ErrorMessage"] = "Payment failed or was cancelled. Please try again.";
                return RedirectToPage("/Orders/Details", new { id = order.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Payment processing error: {ex.Message}";
                return RedirectToPage("/Orders/Index");
            }
        }

        private async Task CreatePendingDeliveryScheduleAsync(Guid orderId)
        {
            var key = GetPendingDeliveryKey(orderId);
            var payload = HttpContext.Session.GetString(key);
            if (string.IsNullOrWhiteSpace(payload))
                return;

            var pending = JsonSerializer.Deserialize<PendingDeliveryInfo>(payload);
            if (pending == null || string.IsNullOrWhiteSpace(pending.Address))
                return;

            var order = await _orderService.GetByIdAsync(orderId);
            if (order.DeliverySchedule != null)
            {
                HttpContext.Session.Remove(key);
                return;
            }

            var deliveryDto = new DeliveryScheduleDto
            {
                OrderId = orderId,
                DeliveryTime = pending.DeliveryTimeUtc,
                Address = pending.Address,
                DriverContact = string.IsNullOrWhiteSpace(pending.DriverContact) ? "TBD" : pending.DriverContact
            };

            await _deliveryService.CreateDeliveryScheduleAsync(orderId, deliveryDto);
            HttpContext.Session.Remove(key);
        }

        private static string GetPendingDeliveryKey(Guid orderId) => $"pending_delivery_{orderId}";

        private sealed class PendingDeliveryInfo
        {
            public string Address { get; set; } = string.Empty;
            public DateTime DeliveryTimeUtc { get; set; }
            public string DriverContact { get; set; } = string.Empty;
        }
    }
}
