using ClosedXML.Excel;
using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class StatisticsModel : PageModel
    {
        private readonly IReportService _reportService;

        public StatisticsModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        // Current view mode
        public string Mode { get; set; } = "month";

        // Filters
        public int Year { get; set; } = DateTime.Now.Year;
        public DateTime DateFrom { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime DateTo { get; set; } = DateTime.Now;

        // Data
        public List<RevenueReportDto> RevenueData { get; set; } = new();
        public List<TopSellingCarDto> TopCars { get; set; } = new();
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();

        // Summary
        public decimal TotalRevenue => RevenueData.Sum(r => r.TotalRevenue);
        public int TotalOrders => RevenueData.Sum(r => r.OrderCount);
        public decimal AvgOrderValue => TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;
        public string HighestPeriod => RevenueData.OrderByDescending(r => r.TotalRevenue).FirstOrDefault()?.Period ?? "—";
        public decimal HighestRevenue => RevenueData.Any() ? RevenueData.Max(r => r.TotalRevenue) : 0;

        public async Task OnGetAsync(string mode = "month", int? year = null, DateTime? from = null, DateTime? to = null)
        {
            Mode = mode;
            Year = year ?? DateTime.Now.Year;
            DateFrom = from ?? DateTime.Now.AddDays(-30);
            DateTo = to ?? DateTime.Now;

            await LoadDataAsync();
        }

        public async Task<IActionResult> OnGetExportAsync(string mode = "month", int? year = null, DateTime? from = null, DateTime? to = null)
        {
            Mode = mode;
            Year = year ?? DateTime.Now.Year;
            DateFrom = from ?? DateTime.Now.AddDays(-30);
            DateTo = to ?? DateTime.Now;

            await LoadDataAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Revenue Report");

            // Title
            var modeLabel = Mode switch
            {
                "day" => $"Doanh thu theo ngày ({DateFrom:dd/MM/yyyy} - {DateTo:dd/MM/yyyy})",
                "month" => $"Doanh thu theo tháng - Năm {Year}",
                "quarter" => $"Doanh thu theo quý - Năm {Year}",
                "year" => "Doanh thu theo năm",
                "custom" => $"Doanh thu tùy chọn ({DateFrom:dd/MM/yyyy} - {DateTo:dd/MM/yyyy})",
                _ => "Revenue Report"
            };
            ws.Cell(1, 1).Value = modeLabel;
            ws.Range(1, 1, 1, 4).Merge().Style.Font.SetBold(true).Font.SetFontSize(14);
            ws.Cell(2, 1).Value = $"Xuất lúc: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 4).Merge().Style.Font.SetItalic(true).Font.SetFontColor(XLColor.Gray);

            // Headers
            int row = 4;
            ws.Cell(row, 1).Value = "Kỳ";
            ws.Cell(row, 2).Value = "Số đơn hàng";
            ws.Cell(row, 3).Value = "Doanh thu (₫)";
            ws.Cell(row, 4).Value = "Giá trị TB (₫)";

            var headerRange = ws.Range(row, 1, row, 4);
            headerRange.Style.Font.SetBold(true);
            headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#ff6b35"));
            headerRange.Style.Font.SetFontColor(XLColor.White);
            headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Data rows
            row++;
            foreach (var item in RevenueData)
            {
                ws.Cell(row, 1).Value = item.Period;
                ws.Cell(row, 2).Value = item.OrderCount;
                ws.Cell(row, 3).Value = item.TotalRevenue;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 4).Value = item.AverageOrderValue;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, 4).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f8f9fa"));

                row++;
            }

            // Totals row
            ws.Cell(row, 1).Value = "TỔNG";
            ws.Cell(row, 1).Style.Font.SetBold(true);
            ws.Cell(row, 2).Value = TotalOrders;
            ws.Cell(row, 2).Style.Font.SetBold(true);
            ws.Cell(row, 3).Value = TotalRevenue;
            ws.Cell(row, 3).Style.Font.SetBold(true).NumberFormat.Format = "#,##0";
            ws.Cell(row, 4).Value = AvgOrderValue;
            ws.Cell(row, 4).Style.Font.SetBold(true).NumberFormat.Format = "#,##0";
            ws.Range(row, 1, row, 4).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#fff3cd"));

            // Borders
            var dataRange = ws.Range(4, 1, row, 4);
            dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Auto-fit
            ws.Columns().AdjustToContents();

            // Generate file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"HyperCar_Revenue_{Mode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task LoadDataAsync()
        {
            RevenueData = Mode switch
            {
                "day" => (await _reportService.GetRevenueByDayAsync(DateFrom, DateTo)).ToList(),
                "month" => (await _reportService.GetRevenueByMonthAsync(Year)).ToList(),
                "quarter" => (await _reportService.GetRevenueByQuarterAsync(Year)).ToList(),
                "year" => (await _reportService.GetRevenueByYearAsync()).ToList(),
                "custom" => (await _reportService.GetRevenueByDayAsync(DateFrom, DateTo)).ToList(),
                _ => new List<RevenueReportDto>()
            };

            // TopCars filtered by the same date range as revenue
            if (Mode == "year")
            {
                // Year mode = all time, no date filter
                TopCars = (await _reportService.GetTopSellingCarsAsync(5)).ToList();
            }
            else
            {
                // Calculate from/to based on mode
                DateTime topFrom, topTo;
                switch (Mode)
                {
                    case "day":
                    case "custom":
                        topFrom = DateFrom;
                        topTo = DateTo;
                        break;
                    case "month":
                    case "quarter":
                        topFrom = new DateTime(Year, 1, 1);
                        topTo = new DateTime(Year, 12, 31, 23, 59, 59);
                        break;
                    default:
                        topFrom = DateTime.MinValue;
                        topTo = DateTime.MaxValue;
                        break;
                }
                TopCars = (await _reportService.GetTopSellingCarsAsync(5, topFrom, topTo)).ToList();
            }

            OrdersByStatus = await _reportService.GetOrdersByStatusAsync();
        }
    }
}
