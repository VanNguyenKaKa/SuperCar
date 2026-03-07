using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthService _authService;
        public LogoutModel(IAuthService authService) => _authService = authService;

        public async Task<IActionResult> OnPostAsync()
        {
            await _authService.LogoutAsync();
            return RedirectToPage("/Index");
        }
    }
}
