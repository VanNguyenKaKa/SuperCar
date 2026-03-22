using HyperCar.DAL.Enums;

namespace HyperCar.BLL.DTOs
{
    public class TestDriveBookingDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? CarImage { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string? Notes { get; set; }
        public string? AdminResponse { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ShowroomId { get; set; }
        public string? ShowroomName { get; set; }
    }

    public class CreateTestDriveDto
    {
        public int CarId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string? Notes { get; set; }
        public int? ShowroomId { get; set; }
    }

    public class TimeSlotDto
    {
        public DateTime SlotTime { get; set; }
        public string DisplayTime => SlotTime.ToString("HH:mm");

        /// <summary>
        /// "available", "booked", or "held"
        /// </summary>
        public string Status { get; set; } = "available";
    }

    public class UpdateBookingStatusDto
    {
        public int BookingId { get; set; }
        public BookingStatus NewStatus { get; set; }
        public string? AdminResponse { get; set; }
    }
}
