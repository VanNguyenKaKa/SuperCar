using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReviewsModel : PageModel
    {
        private readonly IReviewService _reviewService;
        private readonly IHubContext<ReviewHub> _reviewHub;

        public ReviewsModel(IReviewService reviewService, IHubContext<ReviewHub> reviewHub)
        {
            _reviewService = reviewService;
            _reviewHub = reviewHub;
        }

        public IEnumerable<ReviewDto> AllReviews { get; set; } = Enumerable.Empty<ReviewDto>();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            AllReviews = await _reviewService.GetAllAsync();
        }

        public async Task<IActionResult> OnPostToggleAsync(int reviewId)
        {
            // Get review info BEFORE toggling (we need carId + new state)
            var reviewBefore = await _reviewService.GetByIdAsync(reviewId);

            var result = await _reviewService.ToggleReviewVisibilityAsync(reviewId);

            if (result.Success)
            {
                SuccessMessage = "Đã cập nhật trạng thái đánh giá.";

                // ── SignalR: broadcast visibility change ──
                if (reviewBefore != null)
                {
                    var avgRating = await _reviewService.GetAverageRatingAsync(reviewBefore.CarId);
                    var payload = new
                    {
                        ReviewId = reviewId,
                        IsDeleted = !reviewBefore.IsDeleted, // toggled
                        CarId = reviewBefore.CarId,
                        AverageRating = avgRating
                    };

                    await _reviewHub.Clients.Group($"car-{reviewBefore.CarId}")
                        .SendAsync("ReviewVisibilityChanged", payload);
                    await _reviewHub.Clients.Group("AdminReviews")
                        .SendAsync("ReviewVisibilityChanged", payload);
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Có lỗi xảy ra.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveAsync(int reviewId)
        {
            var reviewBefore = await _reviewService.GetByIdAsync(reviewId);

            var result = await _reviewService.ApproveAiFlaggedReviewAsync(reviewId);

            if (result.Success)
            {
                SuccessMessage = "Đã phê duyệt đánh giá — gỡ cờ AI thành công.";

                // ── SignalR: broadcast approval ──
                if (reviewBefore != null)
                {
                    var avgRating = await _reviewService.GetAverageRatingAsync(reviewBefore.CarId);
                    var payload = new
                    {
                        ReviewId = reviewId,
                        CarId = reviewBefore.CarId,
                        AverageRating = avgRating
                    };

                    await _reviewHub.Clients.Group($"car-{reviewBefore.CarId}")
                        .SendAsync("ReviewApproved", payload);
                    await _reviewHub.Clients.Group("AdminReviews")
                        .SendAsync("ReviewApproved", payload);
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Có lỗi xảy ra.";
            }

            return RedirectToPage();
        }
    }
}
