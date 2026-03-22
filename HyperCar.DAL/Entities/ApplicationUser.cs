using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HyperCar.DAL.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber2 { get; set; }

        [MaxLength(500)]
        public string? Avatar { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Test Drive Booking — No-Show strike system
        public int NoShowCount { get; set; } = 0;
        public bool IsBannedFromBooking { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ConversationHistory> ConversationHistories { get; set; } = new List<ConversationHistory>();
        public virtual ICollection<TestDriveBooking> TestDriveBookings { get; set; } = new List<TestDriveBooking>();
    }
}
