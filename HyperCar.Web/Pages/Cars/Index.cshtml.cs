using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Cars
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

        [BindProperty(SupportsGet = true)]
        public CarFilterDto Filter { get; set; } = new();
        public PagedResult<CarDto>? Result { get; set; }
        public IEnumerable<BrandDto>? Brands { get; set; }
        public IEnumerable<string>? Categories { get; set; }

        public async Task OnGetAsync()
        {
            Brands = await _brandService.GetAllAsync();
            Categories = await _carService.GetCategoriesAsync();
            Result = await _carService.GetFilteredAsync(Filter);
        }
    }
}
