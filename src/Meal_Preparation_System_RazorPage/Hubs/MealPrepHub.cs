using Microsoft.AspNetCore.SignalR;

namespace Meal_Preparation_System_RazorPage.Hubs
{
    public class MealPrepHub : Hub
    {
        /// <summary>
        /// Join a user-specific group so they receive their own notifications.
        /// Called by the client after connecting.
        /// </summary>
        public async Task JoinUserGroup(string accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{accountId}");
        }

        /// <summary>
        /// Leave the user-specific group on disconnect or logout.
        /// </summary>
        public async Task LeaveUserGroup(string accountId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{accountId}");
        }

        /// <summary>
        /// Join an order-specific group to receive live status updates for a particular order.
        /// </summary>
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }

        /// <summary>
        /// Join the menu group to receive live updates when the menu changes
        /// (e.g., new meals added, quantities updated, items sold out, menu published).
        /// </summary>
        public async Task JoinMenuGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "menu");
        }

        public async Task LeaveMenuGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "menu");
        }

        /// <summary>
        /// Join the admin group to receive notifications about new orders and system events.
        /// </summary>
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
        }

        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");
        }
    }
}
