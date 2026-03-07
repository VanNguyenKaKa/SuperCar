using HyperCar.DAL.Enums;

namespace HyperCar.BLL.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public OrderStatus Status { get; set; }
        /// <summary>String version of Status for Presentation layer (avoids DAL enum import)</summary>
        public string StatusText => Status.ToString();
        public UserOrderAction UserAction { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public PaymentDto? Payment { get; set; }
        public ShippingDto? Shipping { get; set; }
        public List<TransactionHistoryDto> Timeline { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? CarImage { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    public class CreateOrderDto
    {
        public string? ShippingAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Note { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        /// <summary>
        /// Shipping speed tier: "standard", "express", "hoatoc"
        /// </summary>
        public string ShippingTier { get; set; } = "standard";
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionRef { get; set; }
        public string? BankCode { get; set; }
        public PaymentStatus Status { get; set; }
        /// <summary>String version of Status for Presentation layer</summary>
        public string StatusText => Status.ToString();
        public string? VnPayResponseCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class ShippingDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? TrackingCode { get; set; }
        public decimal Fee { get; set; }
        public ShippingStatus Status { get; set; }
        /// <summary>String version of Status for Presentation layer</summary>
        public string StatusText => Status.ToString();
        public DateTime? EstimatedDelivery { get; set; }
        public string? Address { get; set; }
    }

    public class TransactionHistoryDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? StatusFrom { get; set; }
        public string? StatusTo { get; set; }
        public string? Note { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
