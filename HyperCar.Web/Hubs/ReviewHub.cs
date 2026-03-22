using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Hubs
{
    /// <summary>
    /// Real-time review updates hub.
    /// Clients join a group "car-{carId}" to receive updates for a specific car's reviews.
    /// Admins join "AdminReviews" group automatically.
    /// </summary>
    public class ReviewHub : Hub
    {
        /// <summary>
        /// Client calls this to subscribe to review updates for a specific car
        /// </summary>
        public async Task JoinCarGroup(int carId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"car-{carId}");
        }

        public async Task LeaveCarGroup(int carId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"car-{carId}");
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminReviews");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminReviews");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
