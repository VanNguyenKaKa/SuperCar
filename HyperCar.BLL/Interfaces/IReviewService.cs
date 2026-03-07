using HyperCar.BLL.DTOs;

namespace HyperCar.BLL.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>> GetByCarIdAsync(int carId);
        Task<ReviewDto> CreateAsync(string userId, CreateReviewDto dto);
        Task<bool> DeleteAsync(int reviewId, string userId);
        Task<double> GetAverageRatingAsync(int carId);
    }
}
