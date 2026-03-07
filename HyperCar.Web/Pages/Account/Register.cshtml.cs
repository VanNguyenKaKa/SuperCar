using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;

        public RegisterModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty] public string FullName { get; set; } = string.Empty;
        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string? Phone { get; set; }
        [BindProperty] public string Password { get; set; } = string.Empty;
        [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Password != ConfirmPassword)
            {
                Errors = new List<string> { "Passwords do not match." };
                return Page();
            }

            var (success, errors) = await _authService.RegisterAsync(FullName, Email, Phone, Password);

            if (success)
                return RedirectToPage("/Index");

            Errors = errors;
            return Page();
        }
    }
}
