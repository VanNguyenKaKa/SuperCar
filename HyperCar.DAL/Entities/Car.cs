using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class Car
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int BrandId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int HorsePower { get; set; }

        [MaxLength(200)]
        public string? Engine { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal TopSpeed { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Acceleration { get; set; }

        public int Stock { get; set; }

        [MaxLength(5000)]
        public string? Description { get; set; }
        [MaxLength(5000)]
        public string? DescriptionVi { get; set; }
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        [MaxLength(4000)]
        public string? ImageGallery { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Denormalized average rating, updated transactionally when reviews are added/toggled
        /// </summary>
        [Column(TypeName = "float")]
        public double AverageRating { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(BrandId))]
        public virtual Brand Brand { get; set; } = null!;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<TestDriveBooking> TestDriveBookings { get; set; } = new List<TestDriveBooking>();
    }
}
