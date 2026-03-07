using HyperCar.BLL.DTOs;

namespace HyperCar.BLL.Interfaces
{
    public interface IBrandService
    {
        Task<IEnumerable<BrandDto>> GetAllAsync();
        Task<BrandDto?> GetByIdAsync(int id);
        Task<BrandDto> CreateAsync(BrandDto dto);
        Task<BrandDto> UpdateAsync(int id, BrandDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
