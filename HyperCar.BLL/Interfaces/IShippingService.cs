namespace HyperCar.BLL.Interfaces
{
    public interface IShippingService
    {
        Task<ShippingFeeResult> CalculateFeeAsync(int senderProvinceId, int senderDistrictId,
            int receiverProvinceId, int receiverDistrictId, string shippingTier = "standard", decimal weight = 50);
        Task<List<ProvinceDto>> GetProvincesAsync();
        Task<List<DistrictDto>> GetDistrictsAsync(int provinceCode);
        Task<List<WardDto>> GetWardsAsync(int districtCode);
        Task<string?> CreateShipmentAsync(int orderId, string receiverName, string receiverPhone,
            string address, int provinceId, int districtId, string? wardCode);
        Task<string?> TrackShipmentAsync(string trackingCode);
    }

    public class ProvinceDto
    {
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
    }

    public class DistrictDto
    {
        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
    }

    public class WardDto
    {
        public int WardId { get; set; }
        public string WardName { get; set; } = string.Empty;
        public int DistrictId { get; set; }
    }

    /// <summary>
    /// Result of shipping fee calculation with tier info
    /// </summary>
    public class ShippingFeeResult
    {
        public decimal Fee { get; set; }
        public string Tier { get; set; } = "standard";
        public string TierName { get; set; } = "Tiêu chuẩn";
        public string EstimatedDays { get; set; } = "5-7 ngày";
    }
}
