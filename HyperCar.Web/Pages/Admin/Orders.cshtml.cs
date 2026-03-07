using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using HyperCar.Web.Hubs;

namespace HyperCar.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class OrdersModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OrdersModel(IOrderService orderService, IHubContext<NotificationHub> hubContext)
        {
            _orderService = orderService;
            _hubContext = hubContext;
        }

        public PagedResult<OrderDto>? Orders { get; set; }
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync(int page = 1, string? status = null)
        {
            StatusFilter = status;
            // Use string-based status filter — IOrderService accepts nullable OrderStatus enum
            // Parse through BLL's transitive DAL reference
            HyperCar.DAL.Enums.OrderStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<HyperCar.DAL.Enums.OrderStatus>(status, out var parsed))
                statusEnum = parsed;

            Orders = await _orderService.GetAllOrdersAsync(page, 20, statusEnum);
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
        {
            // Guard: prevent updates on terminal statuses
            var existingOrder = await _orderService.GetByIdAsync(orderId);
            if (existingOrder != null)
            {
                var locked = new[] { "Completed", "Cancelled", "Refunded" };
                if (locked.Contains(existingOrder.StatusText))
                    return RedirectToPage();
            }

            if (Enum.TryParse<HyperCar.DAL.Enums.OrderStatus>(newStatus, out var statusEnum))
            {
                await _orderService.UpdateStatusAsync(orderId, statusEnum, $"Status updated to {newStatus}", "Admin");

                var order = await _orderService.GetByIdAsync(orderId);
                if (order != null)
                {
                    // Notify admin bell
                    await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Order #{orderId} → {newStatus}", "order");

                    // Notify customer bell + toast (Vietnamese)
                    var vnMessage = newStatus switch
                    {
                        "Confirmed" => "Đơn hàng đã được xác nhận",
                        "Processing" => "Đơn hàng đang được xử lý",
                        "Shipping" => "Đơn hàng đang được giao",
                        "Delivered" => "Đơn hàng đã giao thành công. Vui lòng xác nhận đã nhận hàng!",
                        "Cancelled" => "Đơn hàng đã bị hủy",
                        "Refunded" => "Đơn hàng đã được hoàn tiền",
                        _ => $"Đơn hàng đã cập nhật: {newStatus}"
                    };
                    await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveCustomerNotification", $"Đơn hàng #{orderId}: {vnMessage}", "order");
                    await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveOrderUpdate", orderId, newStatus, vnMessage);
                }
            }
            return RedirectToPage();
        }
    }
}
