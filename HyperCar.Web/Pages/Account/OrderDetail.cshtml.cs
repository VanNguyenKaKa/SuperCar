using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HyperCar.BLL.DTOs;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Pages.Account
{
    [Authorize]
    public class OrderDetailModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IAuthService _authService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OrderDetailModel(IOrderService orderService, IAuthService authService,
            IHubContext<NotificationHub> hubContext)
        {
            _orderService = orderService;
            _authService = authService;
            _hubContext = hubContext;
        }

        public OrderDto? Order { get; set; }
        public List<TransactionHistoryDto> Timeline { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = await _authService.GetCurrentUserIdAsync(User);
            if (userId == null) return RedirectToPage("/Account/Login");

            Order = await _orderService.GetByIdAsync(id);

            // Ensure user can only see their own orders (unless admin)
            if (Order == null || (Order.UserId != userId && !User.IsInRole("Admin")))
                return RedirectToPage("/Account/Dashboard");

            Timeline = (await _orderService.GetOrderTimelineAsync(id)).ToList();
            return Page();
        }

        /// <summary>
        /// Customer confirms receipt — Delivered → Completed
        /// </summary>
        public async Task<IActionResult> OnPostConfirmReceivedAsync(int orderId)
        {
            var userId = await _authService.GetCurrentUserIdAsync(User);
            if (userId == null) return RedirectToPage("/Account/Login");

            var success = await _orderService.ConfirmReceivedAsync(orderId, userId);

            if (success)
            {
                // Notify admin
                await _hubContext.Clients.Group("Admins")
                    .SendAsync("ReceiveAdminNotification",
                        $"Customer confirmed receipt for order #{orderId}", "order");

                // Notify customer
                await _hubContext.Clients.User(userId)
                    .SendAsync("ReceiveCustomerNotification",
                        $"Đơn hàng #{orderId} đã hoàn thành. Cảm ơn bạn!", "success");
            }

            return RedirectToPage(new { id = orderId });
        }
    }
}
