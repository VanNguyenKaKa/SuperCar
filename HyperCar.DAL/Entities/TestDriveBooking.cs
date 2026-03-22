using HyperCar.DAL.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class TestDriveBooking
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        public int CarId { get; set; }

        /// <summary>
        /// The exact 1-hour slot start time (e.g. 2026-03-25 10:00)
        /// </summary>
        public DateTime ScheduledDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(1000)]
        public string? AdminResponse { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        public int? ShowroomId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(CarId))]
        public virtual Car Car { get; set; } = null!;

        [ForeignKey(nameof(ShowroomId))]
        public virtual Showroom? Showroom { get; set; }
    }
}
