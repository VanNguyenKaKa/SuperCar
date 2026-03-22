namespace HyperCar.BLL.DTOs
{
    /// <summary>
    /// DTO for eligible order items displayed in the review form dropdown.
    /// Formatted as: "Đơn hàng #1002 - Ngày 22/03/2026"
    /// </summary>
    public class EligibleOrderItemDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Pre-formatted display text for the dropdown
        /// </summary>
        public string DisplayText => $"Đơn hàng #{OrderId} - Ngày {OrderDate:dd/MM/yyyy}";
    }
}
