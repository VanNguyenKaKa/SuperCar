using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.BLL.Services
{
    public class CarService : ICarService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CarService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CarDto?> GetByIdAsync(int id)
        {
            var car = await _unitOfWork.Cars.Query()
                .Include(c => c.Brand)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            return car == null ? null : MapToDto(car);
        }

        public async Task<PagedResult<CarDto>> GetFilteredAsync(CarFilterDto filter)
        {
            var query = _unitOfWork.Cars.Query()
                .Include(c => c.Brand)
                .Include(c => c.Reviews)
                .Where(c => c.IsActive);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search)
                    || (c.Description != null && c.Description.ToLower().Contains(search))
                    || c.Brand.Name.ToLower().Contains(search));
            }

            if (filter.BrandId.HasValue)
                query = query.Where(c => c.BrandId == filter.BrandId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(c => c.Category == filter.Category);

            if (filter.MinPrice.HasValue)
                query = query.Where(c => c.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(c => c.Price <= filter.MaxPrice.Value);

            if (filter.MinHorsePower.HasValue)
                query = query.Where(c => c.HorsePower >= filter.MinHorsePower.Value);

            // Count before paging
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                "price" => filter.SortDescending ? query.OrderByDescending(c => c.Price) : query.OrderBy(c => c.Price),
                "horsepower" => filter.SortDescending ? query.OrderByDescending(c => c.HorsePower) : query.OrderBy(c => c.HorsePower),
                "topspeed" => filter.SortDescending ? query.OrderByDescending(c => c.TopSpeed) : query.OrderBy(c => c.TopSpeed),
                "newest" => query.OrderByDescending(c => c.CreatedDate),
                _ => query.OrderByDescending(c => c.CreatedDate)
            };

            // Apply paging
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<CarDto>
            {
                Items = items.Select(MapToDto),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<IEnumerable<CarDto>> GetFeaturedAsync(int count = 8)
        {
            var cars = await _unitOfWork.Cars.Query()
                .Include(c => c.Brand)
                .Include(c => c.Reviews)
                .Where(c => c.IsActive && c.Stock > 0)
                .OrderByDescending(c => c.CreatedDate)
                .Take(count)
                .ToListAsync();

            return cars.Select(MapToDto);
        }

        public async Task<IEnumerable<CarDto>> GetByCategoryAsync(string category)
        {
            var cars = await _unitOfWork.Cars.Query()
                .Include(c => c.Brand)
                .Where(c => c.IsActive && c.Category == category)
                .ToListAsync();

            return cars.Select(MapToDto);
        }

        public async Task<IEnumerable<CarDto>> SearchAsync(string query, int maxResults = 10)
        {
            var search = query.ToLower();
            var cars = await _unitOfWork.Cars.Query()
                .Include(c => c.Brand)
                .Where(c => c.IsActive &&
                    (c.Name.ToLower().Contains(search) ||
                     c.Brand.Name.ToLower().Contains(search) ||
                     (c.Engine != null && c.Engine.ToLower().Contains(search))))
                .Take(maxResults)
                .ToListAsync();

            return cars.Select(MapToDto);
        }

        public async Task<CarDto> CreateAsync(CarCreateDto dto)
        {
            var car = new Car
            {
                Name = dto.Name,
                BrandId = dto.BrandId,
                Price = dto.Price,
                HorsePower = dto.HorsePower,
                Engine = dto.Engine,
                TopSpeed = dto.TopSpeed,
                Acceleration = dto.Acceleration,
                Stock = dto.Stock,
                Description = dto.Description,
                DescriptionVi = dto.DescriptionVi,
                ImageUrl = dto.ImageUrl,
                ImageGallery = dto.ImageGallery,
                Category = dto.Category,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Cars.AddAsync(car);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(car);
        }

        public async Task<CarDto> UpdateAsync(int id, CarCreateDto dto)
        {
            var car = await _unitOfWork.Cars.GetByIdAsync(id);
            if (car == null) throw new KeyNotFoundException($"Car {id} not found");

            car.Name = dto.Name;
            car.BrandId = dto.BrandId;
            car.Price = dto.Price;
            car.HorsePower = dto.HorsePower;
            car.Engine = dto.Engine;
            car.TopSpeed = dto.TopSpeed;
            car.Acceleration = dto.Acceleration;
            car.Stock = dto.Stock;
            car.Description = dto.Description;
            car.DescriptionVi = dto.DescriptionVi;
            car.ImageUrl = dto.ImageUrl;
            car.ImageGallery = dto.ImageGallery;
            car.Category = dto.Category;
            car.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.Cars.Update(car);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(car);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var car = await _unitOfWork.Cars.GetByIdAsync(id);
            if (car == null) return false;

            // Soft delete
            car.IsActive = false;
            car.UpdatedDate = DateTime.UtcNow;
            _unitOfWork.Cars.Update(car);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockAsync(int carId, int quantity)
        {
            var car = await _unitOfWork.Cars.GetByIdAsync(carId);
            if (car == null) return false;

            car.Stock += quantity;
            if (car.Stock < 0) car.Stock = 0;

            _unitOfWork.Cars.Update(car);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _unitOfWork.Cars.Query()
                .Where(c => c.IsActive && c.Category != null)
                .Select(c => c.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        /// <summary>
        /// Maps Car entity to CarDto with computed review stats
        /// </summary>
        private static CarDto MapToDto(Car car) => new()
        {
            Id = car.Id,
            Name = car.Name,
            BrandId = car.BrandId,
            BrandName = car.Brand?.Name ?? "",
            Price = car.Price,
            HorsePower = car.HorsePower,
            Engine = car.Engine,
            TopSpeed = car.TopSpeed,
            Acceleration = car.Acceleration,
            Stock = car.Stock,
            Description = car.Description,
            DescriptionVi = car.DescriptionVi,
            ImageUrl = car.ImageUrl,
            ImageGallery = car.ImageGallery,
            Category = car.Category,
            IsActive = car.IsActive,
            CreatedDate = car.CreatedDate,
            AverageRating = car.Reviews?.Any() == true ? car.Reviews.Average(r => r.Rating) : 0,
            ReviewCount = car.Reviews?.Count ?? 0
        };
    }
}
