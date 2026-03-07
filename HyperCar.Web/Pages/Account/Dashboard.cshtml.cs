using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Account
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IAuthService _authService;

        public DashboardModel(IOrderService orderService, IAuthService authService)
        {
            _orderService = orderService;
            _authService = authService;
        }

        public IEnumerable<OrderDto>? Orders { get; set; }

        public async Task OnGetAsync()
        {
            var userId = await _authService.GetCurrentUserIdAsync(User);
            if (userId != null)
                Orders = await _orderService.GetUserOrdersAsync(userId);
        }

        /// <summary>
        /// AJAX handler: returns fresh customer stats as JSON for real-time updates
        /// </summary>
        public async Task<IActionResult> OnGetStatsAsync()
        {
            var userId = await _authService.GetCurrentUserIdAsync(User);
            if (userId == null) return new JsonResult(new { });

            var orders = await _orderService.GetUserOrdersAsync(userId);
            var orderList = orders.ToList();

            return new JsonResult(new
            {
                totalOrders = orderList.Count,
                completed = orderList.Count(o => o.StatusText == "Completed"),
                inProgress = orderList.Count(o => o.StatusText == "Pending" || o.StatusText == "Confirmed" || o.StatusText == "Shipping"),
                totalSpent = orderList.Where(o => o.Payment?.StatusText == "Paid").Sum(o => o.TotalAmount)
            });
        }
    }
}
