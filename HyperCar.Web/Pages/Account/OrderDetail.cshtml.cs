using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HyperCar.BLL.DTOs;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HyperCar.Web.Pages.Account
{
    [Authorize]
    public class OrderDetailModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IAuthService _authService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IReviewService _reviewService;
        private readonly IHubContext<ReviewHub> _reviewHub;

        public OrderDetailModel(
            IOrderService orderService,
            IAuthService authService,
            IHubContext<NotificationHub> hubContext,
            IReviewService reviewService,
            IHubContext<ReviewHub> reviewHub)
        {
            _orderService = orderService;
            _authService = authService;
            _hubContext = hubContext;
            _reviewService = reviewService;
            _reviewHub = reviewHub;
        }

        // ── Page Data ──
        public OrderDto? Order { get; set; }
        public List<TransactionHistoryDto> Timeline { get; set; } = new();
        public string? CurrentUserId { get; set; }

        // ── Review Data (per car in this order) ──
        public IEnumerable<EligibleOrderItemDto>? EligibleOrderItems { get; set; }
        public bool CanReview { get; set; }
        public IEnumerable<ReviewDto>? Reviews { get; set; }
        public double AverageRating { get; set; }
        public int ReviewableCarId { get; set; }

        // ── Add Review Form ──
        [BindProperty] public int ReviewCarId { get; set; }
        [BindProperty] public int ReviewOrderItemId { get; set; }
        [BindProperty] public int ReviewRating { get; set; }
        [BindProperty] public string? ReviewComment { get; set; }
        [BindProperty] public IFormFile? ReviewImage1 { get; set; }
        [BindProperty] public IFormFile? ReviewImage2 { get; set; }

        // ── Feedback ──
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (CurrentUserId == null) return RedirectToPage("/Account/Login");

            Order = await _orderService.GetByIdAsync(id);

            // Ensure user can only see their own orders (unless admin)
            if (Order == null || (Order.UserId != CurrentUserId && !User.IsInRole("Admin")))
                return RedirectToPage("/Account/Dashboard");

            Timeline = (await _orderService.GetOrderTimelineAsync(id)).ToList();

            // Load review data only for Completed orders
            if (Order.StatusText == "Completed" && Order.Items.Any())
            {
                // Use the first car in the order for review
                var firstCarId = Order.Items.First().CarId;
                ReviewableCarId = firstCarId;

                EligibleOrderItems = await _reviewService.GetEligibleOrderItemsForReviewAsync(CurrentUserId, firstCarId);
                // Filter to only items belonging to this specific order
                EligibleOrderItems = EligibleOrderItems?
                    .Where(e => Order.Items.Any(i => i.Id == e.OrderItemId))
                    .ToList();
                CanReview = EligibleOrderItems?.Any() == true;

                Reviews = await _reviewService.GetByCarIdAsync(firstCarId, CurrentUserId);
                AverageRating = await _reviewService.GetAverageRatingAsync(firstCarId);
            }

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

        /// <summary>
        /// Customer submits a review from the Order Detail page
        /// </summary>
        public async Task<IActionResult> OnPostReviewAsync(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ErrorMessage = "Vui lòng đăng nhập để đánh giá.";
                return RedirectToPage(new { id = orderId });
            }

            var dto = new CreateReviewDto
            {
                CarId = ReviewCarId,
                OrderItemId = ReviewOrderItemId,
                Rating = ReviewRating,
                Comment = ReviewComment
            };

            var images = new List<IFormFile>();
            if (ReviewImage1 != null) images.Add(ReviewImage1);
            if (ReviewImage2 != null) images.Add(ReviewImage2);

            var result = await _reviewService.AddReviewAsync(userId, dto, images.Count > 0 ? images : null);

            if (result.Success)
            {
                SuccessMessage = "Đánh giá của bạn đã được gửi thành công!";

                var review = result.Data!;
                var avgRating = await _reviewService.GetAverageRatingAsync(ReviewCarId);
                var payload = new
                {
                    review.Id, review.UserId, review.UserName, review.UserAvatar,
                    review.CarId, review.CarName, review.Rating, review.Comment,
                    review.ImageUrls, review.IsDeleted, review.IsEdited,
                    review.IsAiFlagged, review.AiFlagReason, review.OrderItemId,
                    CreatedDate = review.CreatedDate.ToString("dd/MM/yyyy"),
                    AverageRating = avgRating
                };

                await _reviewHub.Clients.Group($"car-{ReviewCarId}").SendAsync("ReviewAdded", payload);
                await _reviewHub.Clients.Group("AdminReviews").SendAsync("ReviewAdded", payload);

                // ── Notify admin bell with link to the Car's Details page ──
                var notifMsg = $"💬 <strong>{review.UserName}</strong> vừa đánh giá xe <strong>{review.CarName}</strong> — <a href='/Cars/Details?id={ReviewCarId}' class='text-accent'>Xem ngay</a>";
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", notifMsg, "review");
            }
            else
            {
                ErrorMessage = result.Error ?? "Có lỗi xảy ra.";
            }

            return RedirectToPage(new { id = orderId });
        }
    }
}
