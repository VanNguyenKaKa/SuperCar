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
    public class BrandsModel : PageModel
    {
        private readonly IBrandService _brandService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ICloudinaryService _cloudinaryService;

        public BrandsModel(IBrandService brandService, IHubContext<NotificationHub> hubContext,
            ICloudinaryService cloudinaryService)
        {
            _brandService = brandService;
            _hubContext = hubContext;
            _cloudinaryService = cloudinaryService;
        }

        public IEnumerable<BrandDto>? Brands { get; set; }

        [BindProperty] public BrandDto BrandInput { get; set; } = new();
        [BindProperty] public int? EditId { get; set; }
        [BindProperty] public IFormFile? BrandLogo { get; set; }

        public async Task OnGetAsync()
        {
            Brands = await _brandService.GetAllAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Upload logo to Cloudinary if provided
            if (BrandLogo != null && BrandLogo.Length > 0)
            {
                var logoUrl = await _cloudinaryService.UploadImageAsync(BrandLogo, "hypercar/brands");
                BrandInput.Logo = logoUrl;
            }

            var brand = await _brandService.CreateAsync(BrandInput);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"New brand added: {brand.Name}", "brand");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (EditId == null) return RedirectToPage();

            // Upload new logo to Cloudinary if provided
            if (BrandLogo != null && BrandLogo.Length > 0)
            {
                var logoUrl = await _cloudinaryService.UploadImageAsync(BrandLogo, "hypercar/brands");
                BrandInput.Logo = logoUrl;
            }
            // If no new logo uploaded, BrandInput.Logo keeps the hidden field value (existing URL)

            await _brandService.UpdateAsync(EditId.Value, BrandInput);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Brand updated: {BrandInput.Name}", "brand");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _brandService.DeleteAsync(id);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Brand deleted (ID: {id})", "brand");
            return RedirectToPage();
        }
    }
}
