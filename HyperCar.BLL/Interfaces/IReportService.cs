using HyperCar.BLL.DTOs;

namespace HyperCar.BLL.Interfaces
{
    public interface IReportService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<IEnumerable<RevenueReportDto>> GetRevenueByDayAsync(DateTime from, DateTime to);
        Task<IEnumerable<RevenueReportDto>> GetRevenueByMonthAsync(int year);
        Task<IEnumerable<RevenueReportDto>> GetRevenueByQuarterAsync(int year);
        Task<IEnumerable<RevenueReportDto>> GetRevenueByYearAsync();
        Task<IEnumerable<TopSellingCarDto>> GetTopSellingCarsAsync(int count = 10);
        Task<Dictionary<string, int>> GetOrdersByStatusAsync();
        Task<double> GetPaymentSuccessRateAsync();
        Task<Dictionary<string, int>> GetShippingPerformanceAsync();
    }
}
