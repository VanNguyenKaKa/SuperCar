using HyperCar.BLL.DTOs;

namespace HyperCar.BLL.Interfaces
{
    public interface ICarService
    {
        Task<CarDto?> GetByIdAsync(int id);
        Task<PagedResult<CarDto>> GetFilteredAsync(CarFilterDto filter);
        Task<IEnumerable<CarDto>> GetFeaturedAsync(int count = 8);
        Task<IEnumerable<CarDto>> GetByCategoryAsync(string category);
        Task<IEnumerable<CarDto>> SearchAsync(string query, int maxResults = 10);
        Task<CarDto> CreateAsync(CarCreateDto dto);
        Task<CarDto> UpdateAsync(int id, CarCreateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateStockAsync(int carId, int quantity);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }
}
