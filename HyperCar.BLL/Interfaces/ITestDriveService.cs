using HyperCar.BLL.DTOs;
using HyperCar.BLL.Helpers;

namespace HyperCar.BLL.Interfaces
{
    public interface ITestDriveService
    {
        /// <summary>
        /// Get 1-hour time slots (08:00–17:00) for a specific car on a specific date.
        /// Returns each slot's availability: "available", "booked", or "held".
        /// </summary>
        Task<IEnumerable<TimeSlotDto>> GetAvailableTimeSlotsAsync(int carId, DateTime date);

        /// <summary>
        /// Soft-lock a time slot in IMemoryCache for 5 minutes.
        /// Broadcasts "ReceiveSlotLocked" via SignalR.
        /// </summary>
        Task<ServiceResult> HoldTimeSlotAsync(string userId, int carId, DateTime scheduledDate);

        /// <summary>
        /// Create a test drive booking. Validates ban status, date, and handles race conditions.
        /// </summary>
        Task<ServiceResult<TestDriveBookingDto>> CreateBookingAsync(string userId, CreateTestDriveDto dto);

        /// <summary>
        /// Get all bookings (admin view)
        /// </summary>
        Task<IEnumerable<TestDriveBookingDto>> GetAllBookingsAsync();

        /// <summary>
        /// Get bookings for a specific user
        /// </summary>
        Task<IEnumerable<TestDriveBookingDto>> GetUserBookingsAsync(string userId);

        /// <summary>
        /// Admin: confirm a pending booking
        /// </summary>
        Task<ServiceResult> ConfirmBookingAsync(int bookingId, string? adminResponse);

        /// <summary>
        /// Admin: mark booking as completed
        /// </summary>
        Task<ServiceResult> CompleteTestDriveAsync(int bookingId);

        /// <summary>
        /// Admin: mark as no-show. Increments user NoShowCount. Bans at 3 strikes.
        /// </summary>
        Task<ServiceResult> MarkAsNoShowAsync(int bookingId);

        /// <summary>
        /// Cancel a booking (user or admin). Removes the soft lock.
        /// </summary>
        Task<ServiceResult> CancelBookingAsync(int bookingId, string userId, bool isAdmin = false);
    }
}
