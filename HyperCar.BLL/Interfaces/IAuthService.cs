using HyperCar.BLL.DTOs;
using System.Security.Claims;

namespace HyperCar.BLL.Interfaces
{
   
    public interface IAuthService
    {
        // === Authentication ===
        Task<bool> LoginAsync(string email, string password, bool rememberMe);
        Task<(bool Success, List<string>? Errors)> RegisterAsync(string fullName, string email, string? phone, string password);
        Task LogoutAsync();
        Task<UserDto?> GetCurrentUserAsync(ClaimsPrincipal user);
        Task<string?> GetCurrentUserIdAsync(ClaimsPrincipal user);
        bool IsSignedIn(ClaimsPrincipal user);
        bool IsInRole(ClaimsPrincipal user, string role);

        // === User Management (Admin) ===
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<(bool Success, List<string>? Errors)> UpdateUserAsync(string userId, string fullName, string email, string? phone);
        Task<bool> DeleteUserAsync(string userId);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> AssignRoleAsync(string userId, string roleName);
        Task<bool> RemoveRoleAsync(string userId, string roleName);

        // === Role Management (Admin) ===
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<(bool Success, string? Error)> CreateRoleAsync(string roleName);
        Task<bool> DeleteRoleAsync(string roleId);

        // === Stats ===
        Task<int> GetTotalUsersCountAsync();
    }

    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }
}
