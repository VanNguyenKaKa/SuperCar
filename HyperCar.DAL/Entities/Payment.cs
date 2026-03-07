using HyperCar.DAL.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [MaxLength(50)]
        public string Method { get; set; } = "VNPay";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string? TransactionRef { get; set; }

        [MaxLength(50)]
        public string? BankCode { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(10)]
        public string? VnPayResponseCode { get; set; }

        [MaxLength(4000)]
        public string? VnPayResponseData { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
