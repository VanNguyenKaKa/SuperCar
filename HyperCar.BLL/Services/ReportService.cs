using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Enums;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.BLL.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public ReportService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsDto
            {
                TotalOrders = await _unitOfWork.Orders.CountAsync(),
                PendingOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                CompletedOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Completed),
                CancelledOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = await _unitOfWork.Orders.Query()
                    .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount),
                TotalCars = await _unitOfWork.Cars.CountAsync(c => c.IsActive),
                TotalCustomers = await _authService.GetTotalUsersCountAsync(),
                RevenueByMonth = (await GetRevenueByMonthAsync(DateTime.UtcNow.Year)).ToList(),
                TopSellingCars = (await GetTopSellingCarsAsync(5)).ToList(),
                OrdersByStatus = await GetOrdersByStatusAsync(),
                PaymentSuccessRate = await GetPaymentSuccessRateAsync(),
                ShippingPerformance = await GetShippingPerformanceAsync()
            };

            return stats;
        }

        public async Task<IEnumerable<RevenueReportDto>> GetRevenueByDayAsync(DateTime from, DateTime to)
        {
            // Materialize first, then format strings in memory
            var rawData = await _unitOfWork.Orders.Query()
                .Where(o => o.CreatedDate >= from && o.CreatedDate <= to &&
                       (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                .GroupBy(o => o.CreatedDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return rawData.Select(r => new RevenueReportDto
            {
                Period = r.Date.ToString("yyyy-MM-dd"),
                TotalRevenue = r.TotalRevenue,
                OrderCount = r.OrderCount,
                AverageOrderValue = r.AverageOrderValue
            });
        }

        public async Task<IEnumerable<RevenueReportDto>> GetRevenueByMonthAsync(int year)
        {
            // Materialize first — EF Core cannot translate string.Format/interpolation
            var rawData = await _unitOfWork.Orders.Query()
                .Where(o => o.CreatedDate.Year == year &&
                       (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                .GroupBy(o => o.CreatedDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(r => r.Month)
                .ToListAsync();

            return rawData.Select(r => new RevenueReportDto
            {
                Period = $"{year}-{r.Month:D2}",
                TotalRevenue = r.TotalRevenue,
                OrderCount = r.OrderCount,
                AverageOrderValue = r.AverageOrderValue
            });
        }

        public async Task<IEnumerable<RevenueReportDto>> GetRevenueByQuarterAsync(int year)
        {
            var orders = await _unitOfWork.Orders.Query()
                .Where(o => o.CreatedDate.Year == year &&
                       (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                .ToListAsync();

            return orders
                .GroupBy(o => (o.CreatedDate.Month - 1) / 3 + 1)
                .Select(g => new RevenueReportDto
                {
                    Period = $"Q{g.Key} {year}",
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(r => r.Period)
                .ToList();
        }

        public async Task<IEnumerable<RevenueReportDto>> GetRevenueByYearAsync()
        {
            var rawData = await _unitOfWork.Orders.Query()
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                .GroupBy(o => o.CreatedDate.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(r => r.Year)
                .ToListAsync();

            return rawData.Select(r => new RevenueReportDto
            {
                Period = r.Year.ToString(),
                TotalRevenue = r.TotalRevenue,
                OrderCount = r.OrderCount,
                AverageOrderValue = r.AverageOrderValue
            });
        }

        public async Task<IEnumerable<TopSellingCarDto>> GetTopSellingCarsAsync(int count = 10)
        {
            // Materialize first to avoid complex GroupBy translation issues
            var orderItems = await _unitOfWork.OrderItems.Query()
                .Include(oi => oi.Car).ThenInclude(c => c.Brand)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == OrderStatus.Completed || oi.Order.Status == OrderStatus.Delivered)
                .ToListAsync();

            return orderItems
                .GroupBy(oi => new { oi.CarId, oi.Car.Name, BrandName = oi.Car.Brand?.Name, oi.Car.ImageUrl })
                .Select(g => new TopSellingCarDto
                {
                    CarId = g.Key.CarId,
                    CarName = g.Key.Name,
                    BrandName = g.Key.BrandName,
                    ImageUrl = g.Key.ImageUrl,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(t => t.TotalSold)
                .Take(count)
                .ToList();
        }

        public async Task<IEnumerable<TopSellingCarDto>> GetTopSellingCarsAsync(int count, DateTime from, DateTime to)
        {
            var orderItems = await _unitOfWork.OrderItems.Query()
                .Include(oi => oi.Car).ThenInclude(c => c.Brand)
                .Include(oi => oi.Order)
                .Where(oi => (oi.Order.Status == OrderStatus.Completed || oi.Order.Status == OrderStatus.Delivered)
                    && oi.Order.CreatedDate >= from && oi.Order.CreatedDate <= to)
                .ToListAsync();

            return orderItems
                .GroupBy(oi => new { oi.CarId, oi.Car.Name, BrandName = oi.Car.Brand?.Name, oi.Car.ImageUrl })
                .Select(g => new TopSellingCarDto
                {
                    CarId = g.Key.CarId,
                    CarName = g.Key.Name,
                    BrandName = g.Key.BrandName,
                    ImageUrl = g.Key.ImageUrl,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(t => t.TotalSold)
                .Take(count)
                .ToList();
        }

        public async Task<Dictionary<string, int>> GetOrdersByStatusAsync()
        {
            var orders = await _unitOfWork.Orders.Query().ToListAsync();

            return Enum.GetValues<OrderStatus>()
                .ToDictionary(
                    s => s.ToString(),
                    s => orders.Count(o => o.Status == s)
                );
        }

        public async Task<double> GetPaymentSuccessRateAsync()
        {
            var total = await _unitOfWork.Payments.CountAsync();
            if (total == 0) return 0;

            var successful = await _unitOfWork.Payments.CountAsync(p => p.Status == PaymentStatus.Paid);
            return Math.Round((double)successful / total * 100, 2);
        }

        public async Task<Dictionary<string, int>> GetShippingPerformanceAsync()
        {
            var shippings = await _unitOfWork.Shippings.Query().ToListAsync();

            return Enum.GetValues<ShippingStatus>()
                .ToDictionary(
                    s => s.ToString(),
                    s => shippings.Count(sh => sh.Status == s)
                );
        }
    }
}
