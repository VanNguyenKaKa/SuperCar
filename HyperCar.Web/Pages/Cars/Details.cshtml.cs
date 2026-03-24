using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Repositories;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace HyperCar.Web.Pages.Cars
{
    public class DetailsModel : PageModel
    {
        private readonly ICarService _carService;
        private readonly IReviewService _reviewService;
        private readonly ITestDriveService _testDriveService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ReviewHub> _reviewHub;
        private readonly IHubContext<NotificationHub> _notifHub;

        public DetailsModel(
            ICarService carService,
            IReviewService reviewService,
            ITestDriveService testDriveService,
            IUnitOfWork unitOfWork,
            IHubContext<ReviewHub> reviewHub,
            IHubContext<NotificationHub> notifHub)
        {
            _carService = carService;
            _reviewService = reviewService;
            _testDriveService = testDriveService;
            _unitOfWork = unitOfWork;
            _reviewHub = reviewHub;
            _notifHub = notifHub;
        }

        // ── Page Data ──
        public CarDto? Car { get; set; }
        public IEnumerable<ReviewDto>? Reviews { get; set; }
        public double AverageRating { get; set; }
        public IEnumerable<EligibleOrderItemDto>? EligibleOrderItems { get; set; }
        public bool CanReview { get; set; }
        public string? CurrentUserId { get; set; }
        public bool IsAdmin { get; set; }

        // ── Showroom list for test drive modal ──
        public List<ShowroomOption> Showrooms { get; set; } = new();

        // ── Add Review Form ──
        [BindProperty] public int ReviewCarId { get; set; }
        [BindProperty] public int ReviewOrderItemId { get; set; }
        [BindProperty] public int ReviewRating { get; set; }
        [BindProperty] public string? ReviewComment { get; set; }
        [BindProperty] public IFormFile? ReviewImage1 { get; set; }
        [BindProperty] public IFormFile? ReviewImage2 { get; set; }

        // ── Edit Review Form ──
        [BindProperty] public int EditReviewId { get; set; }
        [BindProperty] public int EditRating { get; set; }
        [BindProperty] public string? EditComment { get; set; }
        [BindProperty] public IFormFile? EditImage1 { get; set; }
        [BindProperty] public IFormFile? EditImage2 { get; set; }

        // ── Admin Reply Form ──
        [BindProperty] public int AdminReplyReviewId { get; set; }
        [BindProperty] public string? AdminReplyText { get; set; }

        // ── Feedback ──
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Car = await _carService.GetByIdAsync(id);
            if (Car == null) return NotFound();

            CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IsAdmin = User.IsInRole("Admin");

            Reviews = await _reviewService.GetByCarIdAsync(id, CurrentUserId);
            AverageRating = await _reviewService.GetAverageRatingAsync(id);

            if (!string.IsNullOrEmpty(CurrentUserId))
            {
                EligibleOrderItems = await _reviewService.GetEligibleOrderItemsForReviewAsync(CurrentUserId, id);
                CanReview = EligibleOrderItems?.Any() == true;
            }

            // Load showrooms for test drive modal
            Showrooms = await _unitOfWork.Showrooms.Query()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new ShowroomOption { Id = s.Id, Name = s.Name, Address = s.Address })
                .ToListAsync();

            return Page();
        }

        // ══════════════════════════════════════════════════════
        // AJAX GET: Available time slots
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> OnGetAvailableSlotsAsync(int carId, DateTime date)
        {
            var slots = await _testDriveService.GetAvailableTimeSlotsAsync(carId, date);
            return new JsonResult(new { success = true, slots });
        }

        // ══════════════════════════════════════════════════════
        // AJAX POST: Hold time slot (soft lock)
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> OnPostHoldSlotAsync(int carId, string scheduledDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, error = "Vui lòng đăng nhập." });

            if (!DateTime.TryParse(scheduledDate, out var dt))
                return new JsonResult(new { success = false, error = "Ngày giờ không hợp lệ." });

            var result = await _testDriveService.HoldTimeSlotAsync(userId, carId, dt);

            if (result.Success)
            {
                await _notifHub.Clients.All.SendAsync("ReceiveSlotLocked", carId, scheduledDate);
            }

            return new JsonResult(new { success = result.Success, error = result.Error });
        }

        // ══════════════════════════════════════════════════════
        // AJAX POST: Submit test drive booking
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> OnPostSubmitTestDriveAsync(
            int carId, string scheduledDate, string? notes, int? showroomId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, error = "Vui lòng đăng nhập." });

            if (!DateTime.TryParse(scheduledDate, out var dt))
                return new JsonResult(new { success = false, error = "Ngày giờ không hợp lệ." });

            var dto = new CreateTestDriveDto
            {
                CarId = carId,
                ScheduledDate = dt,
                Notes = notes,
                ShowroomId = showroomId
            };

            var result = await _testDriveService.CreateBookingAsync(userId, dto);

            if (result.Success)
            {
                var data = result.Data!;
                // Admin bell notification
                await _notifHub.Clients.Group("Admins").SendAsync(
                    "ReceiveAdminNotification",
                    $"🚗 Lịch lái thử mới: {data.CarName} — {data.ScheduledDate:dd/MM/yyyy HH:mm}",
                    "car");

                // Slot booked → Details page real-time
                await _notifHub.Clients.All.SendAsync(
                    "ReceiveSlotBooked", carId, scheduledDate);

                // Admin TestDrives page auto-refresh
                await _notifHub.Clients.Group("Admins").SendAsync(
                    "ReceiveNewTestDriveBooking");
            }

            return new JsonResult(new { success = result.Success, error = result.Error, data = result.Data });
        }

        // ── POST: Add new review ──
        public async Task<IActionResult> OnPostReviewAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ErrorMessage = "Vui lòng đăng nhập để đánh giá.";
                return RedirectToPage(new { id = ReviewCarId });
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

                // ── Notify admin bell with link to this car's Details page ──
                var notifMsg = $"💬 <strong>{review.UserName}</strong> vừa đánh giá xe <strong>{review.CarName}</strong> — <a href='/Cars/Details?id={ReviewCarId}' class='text-accent'>Xem ngay</a>";
                await _notifHub.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", notifMsg, "review");
            }
            else
            {
                ErrorMessage = result.Error ?? "Có lỗi xảy ra.";
            }

            return RedirectToPage(new { id = ReviewCarId });
        }

        // ── POST: Admin reply to a review ──
        public async Task<IActionResult> OnPostAdminReplyAsync(int carId)
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            if (string.IsNullOrWhiteSpace(AdminReplyText))
            {
                ErrorMessage = "Nội dung phản hồi không được để trống.";
                return RedirectToPage(new { id = carId });
            }

            var result = await _reviewService.AdminReplyAsync(AdminReplyReviewId, AdminReplyText);

            if (result.Success)
            {
                SuccessMessage = "Đã gửi phản hồi thành công.";
                var review = result.Data!;
                var payload = new
                {
                    review.Id, review.UserId, review.UserName, review.UserAvatar,
                    review.CarId, review.CarName, review.Rating, review.Comment,
                    review.ImageUrls, review.IsDeleted, review.IsEdited,
                    review.IsAiFlagged, review.AiFlagReason, review.OrderItemId,
                    CreatedDate = review.CreatedDate.ToString("dd/MM/yyyy"),
                    AdminReply = review.AdminReply,
                    AdminRepliedAt = review.AdminRepliedAt?.ToString("dd/MM/yyyy HH:mm")
                };

                await _reviewHub.Clients.Group($"car-{carId}").SendAsync("ReviewReplied", payload);
                await _reviewHub.Clients.Group("AdminReviews").SendAsync("ReviewReplied", payload);

                // Notify the customer that admin replied
                await _notifHub.Clients.User(review.UserId).SendAsync("ReceiveCustomerNotification",
                    $"💬 Admin đã phản hồi đánh giá của bạn về xe <strong>{review.CarName}</strong>", "info");
            }
            else
            {
                ErrorMessage = result.Error ?? "Có lỗi xảy ra.";
            }

            return RedirectToPage(new { id = carId });
        }

        // ── POST: Admin toggle review visibility on Details page ──
        public async Task<IActionResult> OnPostAdminToggleAsync(int reviewId, int carId)
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            var reviewBefore = await _reviewService.GetByIdAsync(reviewId);
            var result = await _reviewService.ToggleReviewVisibilityAsync(reviewId);

            if (result.Success && reviewBefore != null)
            {
                var avgRating = await _reviewService.GetAverageRatingAsync(reviewBefore.CarId);
                var payload = new
                {
                    ReviewId = reviewId,
                    IsDeleted = !reviewBefore.IsDeleted,
                    CarId = reviewBefore.CarId,
                    AverageRating = avgRating
                };

                await _reviewHub.Clients.Group($"car-{carId}").SendAsync("ReviewVisibilityChanged", payload);
                await _reviewHub.Clients.Group("AdminReviews").SendAsync("ReviewVisibilityChanged", payload);
            }

            return RedirectToPage(new { id = carId });
        }

        // ── POST: Edit existing review ──
        public async Task<IActionResult> OnPostEditReviewAsync(int carId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ErrorMessage = "Vui lòng đăng nhập.";
                return RedirectToPage(new { id = carId });
            }

            var dto = new EditReviewDto
            {
                ReviewId = EditReviewId,
                Rating = EditRating,
                Comment = EditComment
            };

            var images = new List<IFormFile>();
            if (EditImage1 != null) images.Add(EditImage1);
            if (EditImage2 != null) images.Add(EditImage2);

            var result = await _reviewService.EditReviewAsync(userId, dto, images.Count > 0 ? images : null);

            if (result.Success)
            {
                SuccessMessage = "Đánh giá đã được cập nhật!";

                var review = result.Data!;
                var avgRating = await _reviewService.GetAverageRatingAsync(carId);
                var payload = new
                {
                    review.Id, review.UserId, review.UserName, review.UserAvatar,
                    review.CarId, review.CarName, review.Rating, review.Comment,
                    review.ImageUrls, review.IsDeleted, review.IsEdited,
                    review.IsAiFlagged, review.AiFlagReason, review.OrderItemId,
                    CreatedDate = review.CreatedDate.ToString("dd/MM/yyyy"),
                    UpdatedAt = review.UpdatedAt?.ToString("dd/MM/yyyy HH:mm"),
                    AverageRating = avgRating
                };

                await _reviewHub.Clients.Group($"car-{carId}").SendAsync("ReviewUpdated", payload);
                await _reviewHub.Clients.Group("AdminReviews").SendAsync("ReviewUpdated", payload);
            }
            else
            {
                ErrorMessage = result.Error ?? "Có lỗi xảy ra.";
            }

            return RedirectToPage(new { id = carId });
        }
    }

    public class ShowroomOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
