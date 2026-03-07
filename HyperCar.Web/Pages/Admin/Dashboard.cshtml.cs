using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IReportService _reportService;

        public DashboardModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        public DashboardStatsDto? Stats { get; set; }

        public async Task OnGetAsync()
        {
            Stats = await _reportService.GetDashboardStatsAsync();
        }

        /// <summary>
        /// AJAX handler: returns fresh dashboard stats as JSON for real-time updates
        /// </summary>
        public async Task<IActionResult> OnGetStatsAsync()
        {
            var stats = await _reportService.GetDashboardStatsAsync();
            return new JsonResult(stats);
        }
    }
}
