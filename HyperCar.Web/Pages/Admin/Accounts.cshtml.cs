using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using HyperCar.Web.Hubs;

namespace HyperCar.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AccountsModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AccountsModel(IAuthService authService, IHubContext<NotificationHub> hubContext)
        {
            _authService = authService;
            _hubContext = hubContext;
        }

        public List<UserDto> Users { get; set; } = new();
        public List<RoleDto> AllRoles { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _authService.GetAllUsersAsync();
            AllRoles = await _authService.GetAllRolesAsync();
        }

        public async Task<IActionResult> OnPostUpdateAsync(string userId, string fullName, string email, string? phone)
        {
            await _authService.UpdateUserAsync(userId, fullName, email, phone);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"User updated: {email}", "user");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            await _authService.DeleteUserAsync(userId);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", "User deleted", "user");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string userId, string roleName)
        {
            await _authService.AssignRoleAsync(userId, roleName);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Role '{roleName}' assigned", "role");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveRoleAsync(string userId, string roleName)
        {
            await _authService.RemoveRoleAsync(userId, roleName);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Role '{roleName}' removed", "role");
            return RedirectToPage();
        }
    }
}
