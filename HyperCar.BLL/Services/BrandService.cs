using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.BLL.Services
{
    public class BrandService : IBrandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BrandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<BrandDto>> GetAllAsync()
        {
            var brands = await _unitOfWork.Brands.Query()
                .Include(b => b.Cars)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return brands.Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                Country = b.Country,
                Logo = b.Logo,
                Description = b.Description,
                IsActive = b.IsActive,
                CarCount = b.Cars.Count(c => c.IsActive)
            });
        }

        public async Task<BrandDto?> GetByIdAsync(int id)
        {
            var brand = await _unitOfWork.Brands.Query()
                .Include(b => b.Cars)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null) return null;

            return new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Country = brand.Country,
                Logo = brand.Logo,
                Description = brand.Description,
                IsActive = brand.IsActive,
                CarCount = brand.Cars.Count(c => c.IsActive)
            };
        }

        public async Task<BrandDto> CreateAsync(BrandDto dto)
        {
            var brand = new Brand
            {
                Name = dto.Name,
                Country = dto.Country,
                Logo = dto.Logo,
                Description = dto.Description,
                IsActive = true
            };

            await _unitOfWork.Brands.AddAsync(brand);
            await _unitOfWork.SaveChangesAsync();

            dto.Id = brand.Id;
            return dto;
        }

        public async Task<BrandDto> UpdateAsync(int id, BrandDto dto)
        {
            var brand = await _unitOfWork.Brands.GetByIdAsync(id);
            if (brand == null) throw new KeyNotFoundException($"Brand {id} not found");

            brand.Name = dto.Name;
            brand.Country = dto.Country;
            brand.Logo = dto.Logo;
            brand.Description = dto.Description;

            _unitOfWork.Brands.Update(brand);
            await _unitOfWork.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var brand = await _unitOfWork.Brands.GetByIdAsync(id);
            if (brand == null) return false;

            brand.IsActive = false;
            _unitOfWork.Brands.Update(brand);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
