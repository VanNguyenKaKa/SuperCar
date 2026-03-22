using HyperCar.BLL.DTOs;
using HyperCar.BLL.Helpers;
using Microsoft.AspNetCore.Http;

namespace HyperCar.BLL.Interfaces
{
    public interface IReviewService
    {
        /// <summary>
        /// Get visible reviews for a car.
        /// Shadowban: AI-flagged reviews only visible to the author.
        /// </summary>
        Task<IEnumerable<ReviewDto>> GetByCarIdAsync(int carId, string? currentUserId = null);

        /// <summary>
        /// Get ALL reviews for admin moderation (including soft-deleted)
        /// </summary>
        Task<IEnumerable<ReviewDto>> GetAllAsync();

        /// <summary>
        /// Get a single review by ID
        /// </summary>
        Task<ReviewDto?> GetByIdAsync(int reviewId);

        /// <summary>
        /// Check if a user can review a specific OrderItem
        /// </summary>
        Task<bool> CanUserReviewAsync(string userId, int carId, int orderItemId);

        /// <summary>
        /// Get order items eligible for review (completed orders, not yet reviewed).
        /// All logic in BLL — Web just calls this for dropdown data.
        /// </summary>
        Task<IEnumerable<EligibleOrderItemDto>> GetEligibleOrderItemsForReviewAsync(string userId, int carId);

        /// <summary>
        /// Add a new review with optional image upload.
        /// Transaction: upload images → save review → recalculate Car.AverageRating → commit.
        /// On DB failure: cleans up uploaded images from Cloudinary.
        /// </summary>
        Task<ServiceResult<ReviewDto>> AddReviewAsync(string userId, CreateReviewDto dto, List<IFormFile>? images);

        /// <summary>
        /// One-time edit: user can edit their review only once (IsEdited must be false).
        /// If new images are provided, old images are deleted from Cloudinary first.
        /// Sets IsEdited = true and UpdatedAt = now.
        /// </summary>
        Task<ServiceResult<ReviewDto>> EditReviewAsync(string userId, EditReviewDto dto, List<IFormFile>? newImages);

        /// <summary>
        /// Toggle IsDeleted flag for admin moderation, then recalculate Car.AverageRating
        /// </summary>
        Task<ServiceResult> ToggleReviewVisibilityAsync(int reviewId);

        /// <summary>
        /// Admin approves an AI-flagged review: sets IsAiFlagged=false, recalculates AverageRating.
        /// </summary>
        Task<ServiceResult> ApproveAiFlaggedReviewAsync(int reviewId);

        /// <summary>
        /// Get average rating for a car (from non-deleted reviews)
        /// </summary>
        Task<double> GetAverageRatingAsync(int carId);

        /// <summary>
        /// User hard-delete their own review
        /// </summary>
        Task<bool> DeleteAsync(int reviewId, string userId);
    }
}
