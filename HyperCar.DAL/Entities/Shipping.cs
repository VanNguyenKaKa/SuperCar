using HyperCar.DAL.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class Shipping
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [MaxLength(100)]
        public string Provider { get; set; } = "ViettelPost";

        [MaxLength(100)]
        public string? TrackingCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; }

        public ShippingStatus Status { get; set; } = ShippingStatus.Calculating;

        public DateTime? EstimatedDelivery { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? ReceiverName { get; set; }

        [MaxLength(20)]
        public string? ReceiverPhone { get; set; }

        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        [MaxLength(20)]
        public string? WardCode { get; set; }

        [MaxLength(4000)]
        public string? ApiResponseData { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
