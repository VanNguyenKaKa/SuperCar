using HyperCar.DAL.Enums;

namespace HyperCar.BLL.Helpers
{
    public static class StatusHelper
    {
        public static string ToVietnamese(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Processing => "Đang xử lý",
            OrderStatus.Shipping => "Đang giao",
            OrderStatus.Delivered => "Đã giao",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            OrderStatus.Refunded => "Đã hoàn tiền",
            _ => status.ToString()
        };

        public static string ToVietnamese(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "Chờ thanh toán",
            PaymentStatus.Paid => "Đã thanh toán",
            PaymentStatus.Failed => "Thất bại",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => status.ToString()
        };

        public static string ToVietnamese(ShippingStatus status) => status switch
        {
            ShippingStatus.Calculating => "Đang tính",
            ShippingStatus.WaitingPickup => "Chờ lấy hàng",
            ShippingStatus.Delivering => "Đang vận chuyển",
            ShippingStatus.Delivered => "Đã giao",
            ShippingStatus.Failed => "Thất bại",
            _ => status.ToString()
        };

        /// <summary>
        /// Map English status string to Vietnamese (for SignalR/JS usage)
        /// </summary>
        public static string OrderStatusToVietnamese(string englishStatus) => englishStatus switch
        {
            "Pending" => "Chờ xác nhận",
            "Confirmed" => "Đã xác nhận",
            "Processing" => "Đang xử lý",
            "Shipping" => "Đang giao",
            "Delivered" => "Đã giao",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            "Refunded" => "Đã hoàn tiền",
            _ => englishStatus
        };

        public static string PaymentStatusToVietnamese(string englishStatus) => englishStatus switch
        {
            "Pending" => "Chờ thanh toán",
            "Paid" => "Đã thanh toán",
            "Failed" => "Thất bại",
            "Refunded" => "Đã hoàn tiền",
            _ => englishStatus
        };

        public static string ToVietnamese(BookingStatus status) => status switch
        {
            BookingStatus.Pending => "Chờ xác nhận",
            BookingStatus.Confirmed => "Đã xác nhận",
            BookingStatus.Completed => "Hoàn thành",
            BookingStatus.Cancelled => "Đã hủy",
            BookingStatus.NoShow => "Vắng mặt",
            _ => status.ToString()
        };
    }
}
