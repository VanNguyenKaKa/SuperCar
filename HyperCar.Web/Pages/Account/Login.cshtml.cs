using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;

        public LoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            var success = await _authService.LoginAsync(Email, Password, RememberMe);

            if (success)
            {
                // Check if the logged-in user is an Admin via DB lookup
                // (User.IsInRole won't work on the first request — cookie isn't set yet)
                var user = await _authService.GetUserByEmailAsync(Email);
                if (user != null && user.Roles.Contains("Admin"))
                    return RedirectToPage("/Admin/Dashboard");

                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            ErrorMessage = "Invalid email or password.";
            return Page();
        }
    }
}
