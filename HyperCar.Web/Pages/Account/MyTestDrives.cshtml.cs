using HyperCar.BLL.DTOs;
using HyperCar.BLL.Helpers;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Enums;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HyperCar.Web.Pages.Account
{
    [Authorize]
    public class MyTestDrivesModel : PageModel
    {
        private readonly ITestDriveService _testDriveService;
        private readonly IHubContext<NotificationHub> _notifHub;

        public MyTestDrivesModel(ITestDriveService testDriveService, IHubContext<NotificationHub> notifHub)
        {
            _testDriveService = testDriveService;
            _notifHub = notifHub;
        }

        public IEnumerable<TestDriveBookingDto> Bookings { get; set; } = [];

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            Bookings = await _testDriveService.GetUserBookingsAsync(userId);
        }

        // Customer cancel their own booking
        public async Task<IActionResult> OnPostCancelAsync(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Load booking info BEFORE cancelling (to get car name, date, etc.)
            var allBookings = await _testDriveService.GetUserBookingsAsync(userId);
            var booking = allBookings.FirstOrDefault(b => b.Id == bookingId);

            var result = await _testDriveService.CancelBookingAsync(bookingId, userId);
            if (result.Success)
            {
                SuccessMessage = "Đã hủy lịch lái thử.";

                if (booking != null)
                {
                    // Bell notification → Admin group
                    await _notifHub.Clients.Group("Admins").SendAsync(
                        "ReceiveAdminNotification",
                        $"❌ Khách hàng {booking.UserName} đã hủy lịch lái thử xe {booking.CarName} ngày {booking.ScheduledDate:dd/MM/yyyy HH:mm}.",
                        "danger");

                    // Auto-reload admin TestDrives page
                    await _notifHub.Clients.Group("Admins").SendAsync(
                        "ReceiveTestDriveCancelled");

                    // Slot released → Details page real-time
                    await _notifHub.Clients.All.SendAsync(
                        "ReceiveSlotReleased", booking.CarId, booking.ScheduledDate.ToString("yyyy-MM-ddTHH:mm:ss"));
                }
            }
            else
                ErrorMessage = result.Error;
            return RedirectToPage();
        }

        public static string GetStatusBadge(BookingStatus status) => status switch
        {
            BookingStatus.Pending => "bg-warning text-dark",
            BookingStatus.Confirmed => "bg-info",
            BookingStatus.Completed => "bg-success",
            BookingStatus.Cancelled => "bg-secondary",
            BookingStatus.NoShow => "bg-danger",
            _ => "bg-secondary"
        };

        public static string GetStatusText(BookingStatus status) => StatusHelper.ToVietnamese(status);

        public static string GetStatusIcon(BookingStatus status) => status switch
        {
            BookingStatus.Pending => "fas fa-clock",
            BookingStatus.Confirmed => "fas fa-check-circle",
            BookingStatus.Completed => "fas fa-flag-checkered",
            BookingStatus.Cancelled => "fas fa-times-circle",
            BookingStatus.NoShow => "fas fa-user-slash",
            _ => "fas fa-question-circle"
        };
    }
}
