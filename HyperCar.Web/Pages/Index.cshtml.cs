using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICarService _carService;
        private readonly IBrandService _brandService;

        public IndexModel(ICarService carService, IBrandService brandService)
        {
            _carService = carService;
            _brandService = brandService;
        }

        public IEnumerable<CarDto>? FeaturedCars { get; set; }
        public IEnumerable<BrandDto>? Brands { get; set; }
        public int TotalCars { get; set; }

        public async Task OnGetAsync()
        {
            FeaturedCars = await _carService.GetFeaturedAsync(8);
            Brands = await _brandService.GetAllAsync();
            TotalCars = FeaturedCars?.Count() ?? 0;
        }
    }
}
