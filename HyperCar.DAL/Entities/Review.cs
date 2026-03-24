using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int CarId { get; set; }

        public int? OrderItemId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
        [MaxLength(2000)]
        public string? ImageUrls { get; set; }

        public bool IsDeleted { get; set; } = false;
        public bool IsEdited { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }

        public bool IsAiFlagged { get; set; } = false;
        [MaxLength(500)]
        public string? AiFlagReason { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(2000)]
        public string? AdminReply { get; set; }
        public DateTime? AdminRepliedAt { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(CarId))]
        public virtual Car Car { get; set; } = null!;

        [ForeignKey(nameof(OrderItemId))]
        public virtual OrderItem? OrderItem { get; set; }
    }
}
