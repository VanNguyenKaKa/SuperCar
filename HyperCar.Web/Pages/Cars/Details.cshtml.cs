using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Cars
{
    public class DetailsModel : PageModel
    {
        private readonly ICarService _carService;
        private readonly IReviewService _reviewService;

        public DetailsModel(ICarService carService, IReviewService reviewService)
        {
            _carService = carService;
            _reviewService = reviewService;
        }

        public CarDto? Car { get; set; }
        public IEnumerable<ReviewDto>? Reviews { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Car = await _carService.GetByIdAsync(id);
            if (Car == null) return NotFound();

            Reviews = await _reviewService.GetByCarIdAsync(id);
            return Page();
        }
    }
}
