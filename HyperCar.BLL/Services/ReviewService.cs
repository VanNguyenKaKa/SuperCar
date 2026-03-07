using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ReviewDto>> GetByCarIdAsync(int carId)
        {
            var reviews = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .Where(r => r.CarId == carId && r.IsActive)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User?.FullName ?? "Anonymous",
                UserAvatar = r.User?.Avatar,
                CarId = r.CarId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedDate = r.CreatedDate
            });
        }

        public async Task<ReviewDto> CreateAsync(string userId, CreateReviewDto dto)
        {
            var review = new Review
            {
                UserId = userId,
                CarId = dto.CarId,
                Rating = Math.Clamp(dto.Rating, 1, 5),
                Comment = dto.Comment,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            return new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                CarId = review.CarId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedDate = review.CreatedDate
            };
        }

        public async Task<bool> DeleteAsync(int reviewId, string userId)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null || review.UserId != userId) return false;

            review.IsActive = false;
            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<double> GetAverageRatingAsync(int carId)
        {
            var reviews = await _unitOfWork.Reviews.FindAsync(r => r.CarId == carId && r.IsActive);
            var reviewList = reviews.ToList();
            return reviewList.Any() ? reviewList.Average(r => r.Rating) : 0;
        }
    }
}
