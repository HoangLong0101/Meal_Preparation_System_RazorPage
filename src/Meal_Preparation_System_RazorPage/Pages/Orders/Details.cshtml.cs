using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meal_Preparation_System_RazorPage.Pages.Orders
{
    public class DetailsModel : PageModel
    {
        private readonly IOrderService _orderService;

        public DetailsModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public OrderDto Order { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var accountIdStr = HttpContext.Session.GetString("AccountId");
            if (accountIdStr == null)
                return RedirectToPage("/Account/Login");

            try
            {
                Order = await _orderService.GetByIdAsync(id);
                return Page();
            }
            catch
            {
                return RedirectToPage("/Orders/Index");
            }
        }
    }
}
