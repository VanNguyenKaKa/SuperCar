using HyperCar.BLL.DTOs;
using HyperCar.BLL.Helpers;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Enums;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HyperCar.BLL.Services
{
    public class TestDriveService : ITestDriveService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TestDriveService> _logger;

        // Slot configuration
        private const int SlotStartHour = 8;   // 08:00
        private const int SlotEndHour = 17;    // 17:00 (last slot starts at 16:00)
        private const int HoldTtlMinutes = 5;

        public TestDriveService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<TestDriveService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════
        // Cache key format for slot holds
        // ═══════════════════════════════════════════════════
        private static string HoldCacheKey(int carId, DateTime scheduledDate)
            => $"TestDrive_Hold_{carId}_{scheduledDate:yyyyMMddHHmm}";

        // ═══════════════════════════════════════════════════
        // GET AVAILABLE TIME SLOTS
        // ═══════════════════════════════════════════════════
        public async Task<IEnumerable<TimeSlotDto>> GetAvailableTimeSlotsAsync(int carId, DateTime date)
        {
            var slots = new List<TimeSlotDto>();
            var dateOnly = date.Date;

            // Generate 1-hour slots: 08:00, 09:00, ..., 16:00
            for (int hour = SlotStartHour; hour < SlotEndHour; hour++)
            {
                slots.Add(new TimeSlotDto
                {
                    SlotTime = dateOnly.AddHours(hour),
                    Status = "available"
                });
            }

            // Query DB for booked/confirmed slots (exclude cancelled)
            var bookedSlots = await _unitOfWork.TestDriveBookings.Query()
                .Where(b => b.CarId == carId
                    && b.ScheduledDate.Date == dateOnly
                    && b.Status != BookingStatus.Cancelled)
                .Select(b => b.ScheduledDate)
                .ToListAsync();

            var bookedSet = new HashSet<DateTime>(bookedSlots);

            foreach (var slot in slots)
            {
                if (bookedSet.Contains(slot.SlotTime))
                {
                    slot.Status = "booked";
                }
                else if (_cache.TryGetValue(HoldCacheKey(carId, slot.SlotTime), out _))
                {
                    slot.Status = "held";
                }
            }

            return slots;
        }

        // ═══════════════════════════════════════════════════
        // HOLD TIME SLOT (Soft Lock — IMemoryCache, 5 min TTL)
        // SignalR broadcast handled by Web layer after this returns
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> HoldTimeSlotAsync(string userId, int carId, DateTime scheduledDate)
        {
            var cacheKey = HoldCacheKey(carId, scheduledDate);

            // Check if already held by someone else
            if (_cache.TryGetValue(cacheKey, out string? holdingUserId))
            {
                if (holdingUserId != userId)
                    return ServiceResult.Fail("Khung giờ này đang được giữ bởi người khác. Vui lòng chọn giờ khác.");
            }

            // Check if already booked in DB
            var alreadyBooked = await _unitOfWork.TestDriveBookings.AnyAsync(b =>
                b.CarId == carId
                && b.ScheduledDate == scheduledDate
                && b.Status != BookingStatus.Cancelled);

            if (alreadyBooked)
                return ServiceResult.Fail("Khung giờ này đã có người đặt. Vui lòng chọn giờ khác.");

            // Write to cache
            _cache.Set(cacheKey, userId, TimeSpan.FromMinutes(HoldTtlMinutes));

            _logger.LogInformation("User {UserId} held slot {Slot} for Car {CarId}", userId, scheduledDate, carId);
            return ServiceResult.Ok();
        }

        // ═══════════════════════════════════════════════════
        // CREATE BOOKING (Hard Lock — DB unique index)
        // SignalR broadcast handled by Web layer after this returns
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult<TestDriveBookingDto>> CreateBookingAsync(string userId, CreateTestDriveDto dto)
        {
            // Validate scheduled date is in the future
            if (dto.ScheduledDate <= DateTime.Now)
                return ServiceResult<TestDriveBookingDto>.Fail("Thời gian lái thử phải ở tương lai.");

            // Validate hour is within slot range
            if (dto.ScheduledDate.Hour < SlotStartHour || dto.ScheduledDate.Hour >= SlotEndHour)
                return ServiceResult<TestDriveBookingDto>.Fail($"Khung giờ lái thử từ {SlotStartHour}:00 đến {SlotEndHour - 1}:00.");

            // Check if car exists
            var car = await _unitOfWork.Cars.GetByIdAsync(dto.CarId);
            if (car == null)
                return ServiceResult<TestDriveBookingDto>.Fail("Không tìm thấy xe.");

            // Check user ban status — need to query user through existing navigation
            var user = await FindUserAsync(userId);
            if (user == null)
                return ServiceResult<TestDriveBookingDto>.Fail("Không tìm thấy người dùng.");

            if (user.IsBannedFromBooking)
                return ServiceResult<TestDriveBookingDto>.Fail(
                    $"Tài khoản của bạn đã bị cấm đặt lịch lái thử do vắng mặt {user.NoShowCount} lần.");

            // Check if user already has a pending/confirmed booking for this car on this date
            var existingBooking = await _unitOfWork.TestDriveBookings.AnyAsync(b =>
                b.ApplicationUserId == userId
                && b.CarId == dto.CarId
                && b.ScheduledDate.Date == dto.ScheduledDate.Date
                && b.Status != BookingStatus.Cancelled);

            if (existingBooking)
                return ServiceResult<TestDriveBookingDto>.Fail("Bạn đã có lịch lái thử xe này trong ngày này rồi.");

            var booking = new TestDriveBooking
            {
                ApplicationUserId = userId,
                CarId = dto.CarId,
                ScheduledDate = dto.ScheduledDate,
                Notes = dto.Notes,
                ShowroomId = dto.ShowroomId,
                Status = BookingStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.TestDriveBookings.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Remove cache hold (slot is now formally booked)
                _cache.Remove(HoldCacheKey(dto.CarId, dto.ScheduledDate));

                // Reload with navigation properties for DTO mapping
                var saved = await _unitOfWork.TestDriveBookings.Query()
                    .Include(b => b.User)
                    .Include(b => b.Car)
                    .Include(b => b.Showroom)
                    .FirstOrDefaultAsync(b => b.Id == booking.Id);

                return ServiceResult<TestDriveBookingDto>.Ok(MapToDto(saved ?? booking));
            }
            catch (DbUpdateException ex)
            {
                // Hard lock caught a race condition — composite unique index violation
                _logger.LogWarning(ex, "Race condition: slot {Slot} for car {CarId} already taken",
                    dto.ScheduledDate, dto.CarId);
                return ServiceResult<TestDriveBookingDto>.Fail(
                    "Rất tiếc, khung giờ này vừa được người khác đặt. Vui lòng chọn giờ khác.");
            }
        }

        // ═══════════════════════════════════════════════════
        // GET ALL BOOKINGS (Admin)
        // ═══════════════════════════════════════════════════
        public async Task<IEnumerable<TestDriveBookingDto>> GetAllBookingsAsync()
        {
            var bookings = await _unitOfWork.TestDriveBookings.Query()
                .Include(b => b.User)
                .Include(b => b.Car)
                .Include(b => b.Showroom)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            return bookings.Select(MapToDto);
        }

        // ═══════════════════════════════════════════════════
        // GET USER BOOKINGS
        // ═══════════════════════════════════════════════════
        public async Task<IEnumerable<TestDriveBookingDto>> GetUserBookingsAsync(string userId)
        {
            var bookings = await _unitOfWork.TestDriveBookings.Query()
                .Include(b => b.Car)
                .Include(b => b.User)
                .Include(b => b.Showroom)
                .Where(b => b.ApplicationUserId == userId)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            return bookings.Select(MapToDto);
        }

        // ═══════════════════════════════════════════════════
        // CONFIRM BOOKING (Admin)
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> ConfirmBookingAsync(int bookingId, string? adminResponse)
        {
            var booking = await _unitOfWork.TestDriveBookings.GetByIdAsync(bookingId);
            if (booking == null)
                return ServiceResult.Fail("Không tìm thấy lịch lái thử.");

            if (booking.Status != BookingStatus.Pending)
                return ServiceResult.Fail("Chỉ có thể xác nhận lịch đang chờ duyệt.");

            booking.Status = BookingStatus.Confirmed;
            booking.AdminResponse = adminResponse;
            booking.UpdatedDate = DateTime.UtcNow;
            _unitOfWork.TestDriveBookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        // ═══════════════════════════════════════════════════
        // COMPLETE TEST DRIVE (Admin)
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> CompleteTestDriveAsync(int bookingId)
        {
            var booking = await _unitOfWork.TestDriveBookings.GetByIdAsync(bookingId);
            if (booking == null)
                return ServiceResult.Fail("Không tìm thấy lịch lái thử.");

            if (booking.Status != BookingStatus.Confirmed)
                return ServiceResult.Fail("Chỉ có thể hoàn thành lịch đã xác nhận.");

            booking.Status = BookingStatus.Completed;
            booking.UpdatedDate = DateTime.UtcNow;
            _unitOfWork.TestDriveBookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        // ═══════════════════════════════════════════════════
        // MARK AS NO-SHOW (Admin) — Strike system
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> MarkAsNoShowAsync(int bookingId)
        {
            var booking = await _unitOfWork.TestDriveBookings.Query()
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return ServiceResult.Fail("Không tìm thấy lịch lái thử.");

            if (booking.Status != BookingStatus.Confirmed)
                return ServiceResult.Fail("Chỉ có thể đánh dấu vắng mặt cho lịch đã xác nhận.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Mark as no-show
                booking.Status = BookingStatus.NoShow;
                booking.UpdatedDate = DateTime.UtcNow;
                _unitOfWork.TestDriveBookings.Update(booking);

                // Increment user's no-show count
                var user = booking.User;
                user.NoShowCount++;

                // Ban at 3 strikes
                if (user.NoShowCount >= 3)
                    user.IsBannedFromBooking = true;

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult.Ok();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail("Lỗi khi cập nhật trạng thái.");
            }
        }

        // ═══════════════════════════════════════════════════
        // CANCEL BOOKING
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> CancelBookingAsync(int bookingId, string userId, bool isAdmin = false)
        {
            var booking = await _unitOfWork.TestDriveBookings.GetByIdAsync(bookingId);
            if (booking == null)
                return ServiceResult.Fail("Không tìm thấy lịch lái thử.");

            if (!isAdmin && booking.ApplicationUserId != userId)
                return ServiceResult.Fail("Bạn không có quyền hủy lịch này.");

            if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
                return ServiceResult.Fail("Không thể hủy lịch đã hoàn thành hoặc đã hủy.");

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedDate = DateTime.UtcNow;
            _unitOfWork.TestDriveBookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            // Remove cache hold
            _cache.Remove(HoldCacheKey(booking.CarId, booking.ScheduledDate));

            return ServiceResult.Ok();
        }

        // ═══════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Find ApplicationUser by navigating through existing repos.
        /// BLL doesn't have direct UserManager access.
        /// </summary>
        private async Task<ApplicationUser?> FindUserAsync(string userId)
        {
            // Try via existing bookings
            var user = await _unitOfWork.TestDriveBookings.Query()
                .Where(b => b.ApplicationUserId == userId)
                .Select(b => b.User)
                .FirstOrDefaultAsync();

            if (user != null) return user;

            // Try via Orders
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.UserId == userId);
            if (order?.User != null) return order.User;

            // Try via Reviews
            var review = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            return review?.User;
        }

        private static TestDriveBookingDto MapToDto(TestDriveBooking b)
        {
            return new TestDriveBookingDto
            {
                Id = b.Id,
                UserId = b.ApplicationUserId,
                UserName = b.User?.FullName ?? "Ẩn danh",
                UserEmail = b.User?.Email,
                UserPhone = b.User?.PhoneNumber,
                CarId = b.CarId,
                CarName = b.Car?.Name ?? string.Empty,
                CarImage = b.Car?.ImageUrl,
                ScheduledDate = b.ScheduledDate,
                Notes = b.Notes,
                AdminResponse = b.AdminResponse,
                Status = b.Status,
                CreatedDate = b.CreatedDate,
                ShowroomId = b.ShowroomId,
                ShowroomName = b.Showroom?.Name
            };
        }
    }
}
