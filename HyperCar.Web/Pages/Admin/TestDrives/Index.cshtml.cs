using HyperCar.BLL.Helpers;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Enums;
using HyperCar.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace HyperCar.Web.Pages.Admin.TestDrives
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ITestDriveService _testDriveService;
        private readonly IHubContext<NotificationHub> _notifHub;

        public IndexModel(ITestDriveService testDriveService, IHubContext<NotificationHub> notifHub)
        {
            _testDriveService = testDriveService;
            _notifHub = notifHub;
        }

        public IEnumerable<BLL.DTOs.TestDriveBookingDto> Bookings { get; set; } = [];

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            Bookings = await _testDriveService.GetAllBookingsAsync();
        }

        // ── Confirm (Pending → Confirmed) ──
        public async Task<IActionResult> OnPostConfirmAsync(int bookingId, string? adminResponse)
        {
            var result = await _testDriveService.ConfirmBookingAsync(bookingId, adminResponse);
            if (result.Success)
            {
                SuccessMessage = "Đã xác nhận lịch lái thử.";

                var booking = (await _testDriveService.GetAllBookingsAsync())
                    .FirstOrDefault(b => b.Id == bookingId);
                if (booking != null)
                {
                    // Bell notification → customer
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveCustomerNotification",
                        $"✅ Lịch lái thử xe {booking.CarName} ngày {booking.ScheduledDate:dd/MM/yyyy HH:mm} đã được xác nhận!",
                        "success");

                    // Real-time status update → customer page
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveTestDriveUpdate",
                        bookingId,
                        "Confirmed",
                        GetStatusBadge(BookingStatus.Confirmed),
                        GetStatusText(BookingStatus.Confirmed),
                        "fas fa-check-circle",
                        adminResponse ?? "");
                }
            }
            else ErrorMessage = result.Error;
            return RedirectToPage();
        }

        // ── Complete (Confirmed → Completed) ──
        public async Task<IActionResult> OnPostCompleteAsync(int bookingId)
        {
            var result = await _testDriveService.CompleteTestDriveAsync(bookingId);
            if (result.Success)
            {
                SuccessMessage = "Đã hoàn thành lịch lái thử.";

                var booking = (await _testDriveService.GetAllBookingsAsync())
                    .FirstOrDefault(b => b.Id == bookingId);
                if (booking != null)
                {
                    // Special thank-you bell notification → customer
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveCustomerNotification",
                        $"🎉 Cảm ơn quý khách đã trải nghiệm chiếc {booking.CarName}. Hy vọng quý khách đã có thời gian tuyệt vời cùng HyperCar.",
                        "completed");

                    // Real-time status update → customer page
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveTestDriveUpdate",
                        bookingId,
                        "Completed",
                        GetStatusBadge(BookingStatus.Completed),
                        GetStatusText(BookingStatus.Completed),
                        "fas fa-flag-checkered",
                        "");
                }
            }
            else ErrorMessage = result.Error;
            return RedirectToPage();
        }

        // ── No-Show (Confirmed → NoShow, strike +1) ──
        public async Task<IActionResult> OnPostNoShowAsync(int bookingId)
        {
            var result = await _testDriveService.MarkAsNoShowAsync(bookingId);
            if (result.Success)
            {
                SuccessMessage = "Đã đánh dấu vắng mặt.";

                var booking = (await _testDriveService.GetAllBookingsAsync())
                    .FirstOrDefault(b => b.Id == bookingId);
                if (booking != null)
                {
                    // Bell warning → customer
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveCustomerNotification",
                        $"⚠️ Bạn đã bị đánh dấu vắng mặt cho lịch lái thử xe {booking.CarName} ngày {booking.ScheduledDate:dd/MM/yyyy HH:mm}.",
                        "warning");

                    // Real-time status update → customer page
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveTestDriveUpdate",
                        bookingId,
                        "NoShow",
                        GetStatusBadge(BookingStatus.NoShow),
                        GetStatusText(BookingStatus.NoShow),
                        "fas fa-user-slash",
                        "");
                }
            }
            else ErrorMessage = result.Error;
            return RedirectToPage();
        }

        // ── Cancel ──
        public async Task<IActionResult> OnPostCancelAsync(int bookingId)
        {
            var result = await _testDriveService.CancelBookingAsync(bookingId, "", isAdmin: true);
            if (result.Success)
            {
                SuccessMessage = "Đã hủy lịch lái thử.";

                var booking = (await _testDriveService.GetAllBookingsAsync())
                    .FirstOrDefault(b => b.Id == bookingId);
                if (booking != null)
                {
                    // Bell notification → customer
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveCustomerNotification",
                        $"❌ Lịch lái thử xe {booking.CarName} ngày {booking.ScheduledDate:dd/MM/yyyy HH:mm} đã bị hủy bởi admin.",
                        "danger");

                    // Real-time status update → customer page
                    await _notifHub.Clients.User(booking.UserId).SendAsync(
                        "ReceiveTestDriveUpdate",
                        bookingId,
                        "Cancelled",
                        GetStatusBadge(BookingStatus.Cancelled),
                        GetStatusText(BookingStatus.Cancelled),
                        "fas fa-times-circle",
                        "");

                    // Slot released → Details page
                    await _notifHub.Clients.All.SendAsync(
                        "ReceiveSlotReleased", booking.CarId, booking.ScheduledDate.ToString("yyyy-MM-ddTHH:mm:ss"));
                }
            }
            else ErrorMessage = result.Error;
            return RedirectToPage();
        }

        // ── Helpers for View ──
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
    }
}
