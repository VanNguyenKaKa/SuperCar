using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using HyperCar.Web.Hubs;

namespace HyperCar.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RolesModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public RolesModel(IAuthService authService, IHubContext<NotificationHub> hubContext)
        {
            _authService = authService;
            _hubContext = hubContext;
        }

        public List<RoleDto> Roles { get; set; } = new();

        [BindProperty] public string NewRoleName { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Roles = await _authService.GetAllRolesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewRoleName))
            {
                await _authService.CreateRoleAsync(NewRoleName.Trim());
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Role created: {NewRoleName}", "role");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string roleId)
        {
            await _authService.DeleteRoleAsync(roleId);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", "Role deleted", "role");
            return RedirectToPage();
        }
    }
}
