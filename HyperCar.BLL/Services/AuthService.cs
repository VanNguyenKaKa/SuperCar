using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HyperCar.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ===================== Authentication =====================

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
            return result.Succeeded;
        }

        public async Task<(bool Success, List<string>? Errors)> RegisterAsync(string fullName, string email, string? phone, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phone,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return (true, null);
            }

            return (false, result.Errors.Select(e => e.Description).ToList());
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<UserDto?> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            var appUser = await _userManager.GetUserAsync(user);
            return appUser == null ? null : MapToDto(appUser);
        }

        public async Task<string?> GetCurrentUserIdAsync(ClaimsPrincipal user)
        {
            var appUser = await _userManager.GetUserAsync(user);
            return appUser?.Id;
        }

        public bool IsSignedIn(ClaimsPrincipal user) => _signInManager.IsSignedIn(user);
        public bool IsInRole(ClaimsPrincipal user, string role) => user.IsInRole(role);

        // ===================== User Management =====================

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedDate).ToListAsync();
            var result = new List<UserDto>();

            foreach (var u in users)
            {
                var dto = MapToDto(u);
                var roles = await _userManager.GetRolesAsync(u);
                dto.Roles = roles.ToList();
                result.Add(dto);
            }
            return result;
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var dto = MapToDto(user);
            dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            return dto;
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var dto = MapToDto(user);
            dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            return dto;
        }

        public async Task<(bool Success, List<string>? Errors)> UpdateUserAsync(string userId, string fullName, string email, string? phone)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, new List<string> { "User not found." });

            user.FullName = fullName;
            user.Email = email;
            user.UserName = email;
            user.PhoneNumber = phone;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? (true, null)
                : (false, result.Errors.Select(e => e.Description).ToList());
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();
            return (await _userManager.GetRolesAsync(user)).ToList();
        }

        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            if (!await _roleManager.RoleExistsAsync(roleName)) return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public async Task<bool> RemoveRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }

        // ===================== Role Management =====================

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var result = new List<RoleDto>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                result.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    UserCount = usersInRole.Count
                });
            }
            return result;
        }

        public async Task<(bool Success, string? Error)> CreateRoleAsync(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                return (false, "Role already exists.");

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            return result.Succeeded
                ? (true, null)
                : (false, result.Errors.FirstOrDefault()?.Description);
        }

        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return false;

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded;
        }

        // ===================== Stats =====================

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _userManager.Users.CountAsync();
        }

        // ===================== Helper =====================

        private static UserDto MapToDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.Avatar,
            Address = user.Address
        };
    }
}
