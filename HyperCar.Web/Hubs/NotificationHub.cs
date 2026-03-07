using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendOrderUpdate(string userId, int orderId, string status, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveOrderUpdate", orderId, status, message);
        }
        public async Task SendPaymentConfirmation(string userId, int orderId, string status)
        {
            await Clients.User(userId).SendAsync("ReceivePaymentConfirmation", orderId, status);
        }
        public async Task SendShippingUpdate(string userId, int orderId, string status, string? trackingCode)
        {
            await Clients.User(userId).SendAsync("ReceiveShippingUpdate", orderId, status, trackingCode);
        }
        public async Task SendCustomerNotification(string userId, string message, string type)
        {
            await Clients.User(userId).SendAsync("ReceiveCustomerNotification", message, type);
        }
        public async Task NotifyAdmins(string message, string type)
        {
            await Clients.Group("Admins").SendAsync("ReceiveAdminNotification", message, type);
        }
        public override async Task OnConnectedAsync()
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
