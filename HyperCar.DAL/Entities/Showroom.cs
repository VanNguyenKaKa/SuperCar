using System.ComponentModel.DataAnnotations;

namespace HyperCar.DAL.Entities
{
    public class Showroom
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<TestDriveBooking> TestDriveBookings { get; set; } = new List<TestDriveBooking>();
    }
}
