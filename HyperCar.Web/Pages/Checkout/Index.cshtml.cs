using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Pages.Checkout
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IVNPayService _vnPayService;
        private readonly IShippingService _shippingService;
        private readonly IAuthService _authService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IndexModel(ICartService cartService, IOrderService orderService,
            IVNPayService vnPayService, IShippingService shippingService,
            IAuthService authService, IHubContext<NotificationHub> hubContext)
        {
            _cartService = cartService;
            _orderService = orderService;
            _vnPayService = vnPayService;
            _shippingService = shippingService;
            _authService = authService;
            _hubContext = hubContext;
        }

        public CartDto? Cart { get; set; }
        public List<ProvinceDto> Provinces { get; set; } = new();

        [BindProperty]
        public CreateOrderDto OrderDto { get; set; } = new();

        public async Task OnGetAsync()
        {
            Cart = _cartService.GetCart(HttpContext.Session);
            Provinces = await _shippingService.GetProvincesAsync();
        }

        /// <summary>
        /// Creates order, sends real-time notifications, generates VNPay payment URL, and redirects user
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            Cart = _cartService.GetCart(HttpContext.Session);
            if (Cart?.Items?.Any() != true) return RedirectToPage("/Cart/Index");

            var userId = await _authService.GetCurrentUserIdAsync(User);
            if (userId == null) return RedirectToPage("/Account/Login");

            // Create order
            var order = await _orderService.CreateOrderAsync(userId, OrderDto, Cart);

            // === Real-time SignalR notifications ===
            // Notify Customer bell
            await _hubContext.Clients.User(userId)
                .SendAsync("ReceiveCustomerNotification",
                    $"Đơn hàng #{order.Id} đã được tạo thành công! Đang chờ thanh toán.", "order");

            // Notify Admin bell + dashboard
            await _hubContext.Clients.Group("Admins")
                .SendAsync("ReceiveAdminNotification",
                    $"Đơn hàng mới #{order.Id} — ${order.TotalAmount:N0}", "order");

            // Clear cart
            _cartService.ClearCart(HttpContext.Session);

            // Generate VNPay URL and redirect
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = _vnPayService.CreatePaymentUrl(order.Id, order.TotalAmount,
                $"HyperCar Order #{order.Id}", ipAddress);

            return Redirect(paymentUrl);
        }

        // =============== AJAX HANDLERS ===============

        /// <summary>
        /// AJAX: Get districts for a province
        /// </summary>
        public async Task<IActionResult> OnGetDistricts(int provinceId)
        {
            var districts = await _shippingService.GetDistrictsAsync(provinceId);
            return new JsonResult(districts);
        }

        /// <summary>
        /// AJAX: Get wards for a district
        /// </summary>
        public async Task<IActionResult> OnGetWards(int districtId)
        {
            var wards = await _shippingService.GetWardsAsync(districtId);
            return new JsonResult(wards);
        }

        /// <summary>
        /// AJAX: Calculate shipping fee with tier
        /// </summary>
        public async Task<IActionResult> OnPostCalculateShipping(int provinceId, int districtId, string shippingTier)
        {
            var senderProvince = 79; // HCM
            var senderDistrict = 760;
            var result = await _shippingService.CalculateFeeAsync(
                senderProvince, senderDistrict, provinceId, districtId, shippingTier ?? "standard");
            return new JsonResult(new { success = true, fee = result.Fee, tierName = result.TierName, estimatedDays = result.EstimatedDays });
        }
    }
}
