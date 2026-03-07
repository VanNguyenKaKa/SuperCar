namespace HyperCar.BLL.DTOs
{
    /// <summary>
    /// Revenue report DTO with aggregated financial data
    /// </summary>
    public class RevenueReportDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class TopSellingCarDto
    {
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalCars { get; set; }
        public double PaymentSuccessRate { get; set; }
        public List<RevenueReportDto> RevenueByMonth { get; set; } = new();
        public List<TopSellingCarDto> TopSellingCars { get; set; } = new();
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
        public Dictionary<string, int> ShippingPerformance { get; set; } = new();
    }
}
