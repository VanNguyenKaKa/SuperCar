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
    public class CarsModel : PageModel
    {
        private readonly ICarService _carService;
        private readonly IBrandService _brandService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ICloudinaryService _cloudinaryService;

        public CarsModel(ICarService carService, IBrandService brandService,
            IHubContext<NotificationHub> hubContext, ICloudinaryService cloudinaryService)
        {
            _carService = carService;
            _brandService = brandService;
            _hubContext = hubContext;
            _cloudinaryService = cloudinaryService;
        }

        public PagedResult<CarDto>? Cars { get; set; }
        public IEnumerable<BrandDto>? Brands { get; set; }

        [BindProperty] public CarCreateDto CarInput { get; set; } = new();
        [BindProperty] public int? EditId { get; set; }
        [BindProperty] public IFormFile? CarImage { get; set; }

        public async Task OnGetAsync(int page = 1, string? search = null)
        {
            Cars = await _carService.GetFilteredAsync(new CarFilterDto { Page = page, PageSize = 12, SearchTerm = search });
            Brands = await _brandService.GetAllAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Upload image to Cloudinary if provided
            if (CarImage != null && CarImage.Length > 0)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(CarImage, "hypercar/cars");
                CarInput.ImageUrl = imageUrl;
            }

            var car = await _carService.CreateAsync(CarInput);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"New car added: {car.Name}", "car");
            // Broadcast to all clients so public Collection page updates
            await _hubContext.Clients.All.SendAsync("ReceiveCollectionUpdate", "car");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (EditId == null) return RedirectToPage();

            // Upload new image to Cloudinary if provided
            if (CarImage != null && CarImage.Length > 0)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(CarImage, "hypercar/cars");
                CarInput.ImageUrl = imageUrl;
            }
            // If no new image uploaded, CarInput.ImageUrl keeps the hidden field value (existing URL)

            await _carService.UpdateAsync(EditId.Value, CarInput);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Car updated: {CarInput.Name}", "car");
            await _hubContext.Clients.All.SendAsync("ReceiveCollectionUpdate", "car");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _carService.DeleteAsync(id);
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveAdminNotification", $"Car deleted (ID: {id})", "car");
            await _hubContext.Clients.All.SendAsync("ReceiveCollectionUpdate", "car");
            return RedirectToPage();
        }
    }
}
