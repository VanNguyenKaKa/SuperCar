using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Enums;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Pages.Payment
{
    /// <summary>
    /// Handles VNPay callback — validates signature, updates payment/order, fires SignalR notification.
    /// On payment failure: auto-cancels order + restores stock (business logic).
    /// </summary>
    public class VNPayReturnModel : PageModel
    {
        private readonly IVNPayService _vnPayService;
        private readonly IOrderService _orderService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public VNPayReturnModel(IVNPayService vnPayService, IOrderService orderService,
            IHubContext<NotificationHub> hubContext)
        {
            _vnPayService = vnPayService;
            _orderService = orderService;
            _hubContext = hubContext;
        }

        public bool IsSuccess { get; set; }
        public int OrderId { get; set; }
        public string? TransactionRef { get; set; }
        public string? ResponseCode { get; set; }
        public decimal Amount { get; set; }

        public async Task OnGetAsync()
        {
            // Extract query parameters from VNPay callback
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

            // Validate VNPay response and signature
            var paymentResult = await _vnPayService.ValidateResponseAsync(queryParams);

            OrderId = paymentResult.OrderId;
            TransactionRef = paymentResult.TransactionRef;
            ResponseCode = paymentResult.VnPayResponseCode;
            Amount = paymentResult.Amount;
            IsSuccess = paymentResult.StatusText == "Paid";

            // Update payment record
            await _vnPayService.UpdatePaymentAsync(OrderId, paymentResult);

            var order = await _orderService.GetByIdAsync(OrderId);

            if (IsSuccess)
            {
                // === Payment SUCCESS ===
                // Update order status to Confirmed (paid & ready for processing)
                await _orderService.UpdateStatusAsync(OrderId, OrderStatus.Confirmed,
                    "Thanh toán thành công qua VNPay", "VNPay");

                if (order != null)
                {
                    // Notify customer bell + toast
                    await _hubContext.Clients.User(order.UserId)
                        .SendAsync("ReceiveCustomerNotification",
                            $"Đơn hàng #{OrderId}: Thanh toán thành công! Đơn hàng đã được xác nhận.", "payment");
                    await _hubContext.Clients.User(order.UserId)
                        .SendAsync("ReceivePaymentConfirmation", OrderId, "Paid");

                    // Notify admin bell + dashboard refresh
                    await _hubContext.Clients.Group("Admins")
                        .SendAsync("ReceiveAdminNotification",
                            $"💰 Đơn hàng #{OrderId} đã thanh toán — ${order.TotalAmount:N0}", "payment");
                }
            }
            else
            {
                // === Payment FAILED ===
                // Cancel order + restore stock (business logic: unpaid order = cancelled)
                if (order != null)
                {
                    await _orderService.UpdateStatusAsync(OrderId, OrderStatus.Cancelled,
                        $"Thanh toán thất bại (VNPay code: {ResponseCode})", "VNPay");

                    // Notify customer
                    await _hubContext.Clients.User(order.UserId)
                        .SendAsync("ReceiveCustomerNotification",
                            $"Đơn hàng #{OrderId}: Thanh toán thất bại. Đơn hàng đã bị hủy.", "danger");

                    // Notify admin
                    await _hubContext.Clients.Group("Admins")
                        .SendAsync("ReceiveAdminNotification",
                            $"❌ Đơn hàng #{OrderId} thanh toán thất bại — đã hủy", "order");
                }
            }
        }
    }
}
