using HyperCar.DAL.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public UserOrderAction UserAction { get; set; } = UserOrderAction.None;

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(100)]
        public string? ReceiverName { get; set; }

        [MaxLength(20)]
        public string? ReceiverPhone { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment? Payment { get; set; }
        public virtual Shipping? Shipping { get; set; }
        public virtual ICollection<TransactionHistory> TransactionHistories { get; set; } = new List<TransactionHistory>();
    }
}
