using HyperCar.BLL.DTOs;
using HyperCar.BLL.Helpers;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Enums;
using HyperCar.DAL.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HyperCar.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IAIModerationService _moderationService;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 2 * 1024 * 1024;
        private const int MaxImageCount = 2;
        private const string CloudinaryFolder = "hypercar/reviews";

        public ReviewService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IAIModerationService moderationService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _moderationService = moderationService;
        }

        // ═══════════════════════════════════════════════════
        // GET REVIEWS (public — shadowban LINQ)
        // Flagged reviews only visible to their author
        // ═══════════════════════════════════════════════════
        public async Task<IEnumerable<ReviewDto>> GetByCarIdAsync(int carId, string? currentUserId = null)
        {
            var reviews = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .Where(r => r.CarId == carId
                    && !r.IsDeleted
                    && (!r.IsAiFlagged || r.UserId == currentUserId))
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(MapToDto);
        }

        // ═══════════════════════════════════════════════════
        // GET ALL REVIEWS (admin — everything)
        // ═══════════════════════════════════════════════════
        public async Task<IEnumerable<ReviewDto>> GetAllAsync()
        {
            var reviews = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .Include(r => r.Car)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(MapToDto);
        }

        // ═══════════════════════════════════════════════════
        // GET SINGLE REVIEW BY ID
        // ═══════════════════════════════════════════════════
        public async Task<ReviewDto?> GetByIdAsync(int reviewId)
        {
            var review = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            return review != null ? MapToDto(review) : null;
        }

        // ═══════════════════════════════════════════════════
        // ELIGIBILITY CHECK
        // ═══════════════════════════════════════════════════
        public async Task<bool> CanUserReviewAsync(string userId, int carId, int orderItemId)
        {
            var orderItem = await _unitOfWork.OrderItems.Query()
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi =>
                    oi.Id == orderItemId &&
                    oi.CarId == carId &&
                    oi.Order.UserId == userId &&
                    oi.Order.Status == OrderStatus.Completed);

            if (orderItem == null) return false;

            var alreadyReviewed = await _unitOfWork.Reviews.AnyAsync(r => r.OrderItemId == orderItemId);
            return !alreadyReviewed;
        }

        // ═══════════════════════════════════════════════════
        // GET ELIGIBLE ORDER ITEMS
        // ═══════════════════════════════════════════════════
        public async Task<IEnumerable<EligibleOrderItemDto>> GetEligibleOrderItemsForReviewAsync(string userId, int carId)
        {
            var completedItems = await _unitOfWork.OrderItems.Query()
                .Include(oi => oi.Order)
                .Include(oi => oi.Car)
                .Where(oi =>
                    oi.CarId == carId &&
                    oi.Order.UserId == userId &&
                    oi.Order.Status == OrderStatus.Completed)
                .ToListAsync();

            var reviewedItemIds = await _unitOfWork.Reviews.Query()
                .Where(r => r.OrderItemId != null)
                .Select(r => r.OrderItemId!.Value)
                .ToListAsync();

            var reviewedSet = new HashSet<int>(reviewedItemIds);

            return completedItems
                .Where(oi => !reviewedSet.Contains(oi.Id))
                .Select(oi => new EligibleOrderItemDto
                {
                    OrderItemId = oi.Id,
                    OrderId = oi.OrderId,
                    CarName = oi.Car.Name,
                    OrderDate = oi.Order.CreatedDate
                })
                .OrderByDescending(dto => dto.OrderDate);
        }

        // ═══════════════════════════════════════════════════
        // ADD REVIEW (AI moderation → image upload → transaction)
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult<ReviewDto>> AddReviewAsync(string userId, CreateReviewDto dto, List<IFormFile>? images)
        {
            if (!await CanUserReviewAsync(userId, dto.CarId, dto.OrderItemId))
                return ServiceResult<ReviewDto>.Fail("Bạn không đủ điều kiện đánh giá sản phẩm này hoặc đã đánh giá rồi.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return ServiceResult<ReviewDto>.Fail("Rating phải từ 1 đến 5.");

            // ── AI Moderation — BEFORE saving ──
            bool isAiFlagged = false;
            string? aiFlagReason = null;
            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                var moderationResult = await _moderationService.AnalyzeReviewAsync(dto.Comment);
                if (!moderationResult.IsClean)
                {
                    isAiFlagged = true;
                    aiFlagReason = moderationResult.Reason;
                }
            }

            // ── Validate & upload images ──
            var uploadedUrls = new List<string>();
            var validationError = ValidateImageFiles(images);
            if (validationError != null)
                return ServiceResult<ReviewDto>.Fail(validationError);

            if (images != null && images.Count > 0)
            {
                var uploadTasks = images.Select(f => _cloudinaryService.UploadImageAsync(f, CloudinaryFolder));
                var results = await Task.WhenAll(uploadTasks);
                uploadedUrls.AddRange(results);
            }

            // ── Save in transaction ──
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var review = new Review
                {
                    UserId = userId,
                    CarId = dto.CarId,
                    OrderItemId = dto.OrderItemId,
                    Rating = Math.Clamp(dto.Rating, 1, 5),
                    Comment = dto.Comment,
                    ImageUrls = uploadedUrls.Count > 0 ? JsonSerializer.Serialize(uploadedUrls) : null,
                    IsDeleted = false,
                    IsEdited = false,
                    IsAiFlagged = isAiFlagged,
                    AiFlagReason = aiFlagReason,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                // Only recalculate AverageRating if review is clean
                if (!isAiFlagged)
                {
                    await RecalculateAverageRatingAsync(dto.CarId);
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                var saved = await _unitOfWork.Reviews.Query()
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == review.Id);

                return ServiceResult<ReviewDto>.Ok(MapToDto(saved ?? review));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                await CleanupCloudinaryImagesAsync(uploadedUrls);
                return ServiceResult<ReviewDto>.Fail("Lỗi khi lưu đánh giá. Vui lòng thử lại.");
            }
        }

        // ═══════════════════════════════════════════════════
        // EDIT REVIEW (one-time, with AI re-moderation)
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult<ReviewDto>> EditReviewAsync(string userId, EditReviewDto dto, List<IFormFile>? newImages)
        {
            var review = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == dto.ReviewId);

            if (review == null)
                return ServiceResult<ReviewDto>.Fail("Không tìm thấy đánh giá.");
            if (review.UserId != userId)
                return ServiceResult<ReviewDto>.Fail("Bạn không có quyền chỉnh sửa đánh giá này.");
            if (review.IsEdited)
                return ServiceResult<ReviewDto>.Fail("Bạn đã chỉnh sửa đánh giá này rồi. Mỗi đánh giá chỉ được sửa 1 lần.");
            if (dto.Rating < 1 || dto.Rating > 5)
                return ServiceResult<ReviewDto>.Fail("Rating phải từ 1 đến 5.");

            // ── AI Moderation on edited content ──
            bool isAiFlagged = false;
            string? aiFlagReason = null;
            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                var moderationResult = await _moderationService.AnalyzeReviewAsync(dto.Comment);
                if (!moderationResult.IsClean)
                {
                    isAiFlagged = true;
                    aiFlagReason = moderationResult.Reason;
                }
            }

            var validationError = ValidateImageFiles(newImages);
            if (validationError != null)
                return ServiceResult<ReviewDto>.Fail(validationError);

            var oldImageUrls = new List<string>();
            if (!string.IsNullOrEmpty(review.ImageUrls))
            {
                try { oldImageUrls = JsonSerializer.Deserialize<List<string>>(review.ImageUrls) ?? new(); }
                catch { }
            }

            var newUploadedUrls = new List<string>();
            if (newImages != null && newImages.Count > 0)
            {
                var uploadTasks = newImages.Select(f => _cloudinaryService.UploadImageAsync(f, CloudinaryFolder));
                var results = await Task.WhenAll(uploadTasks);
                newUploadedUrls.AddRange(results);
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                review.Rating = Math.Clamp(dto.Rating, 1, 5);
                review.Comment = dto.Comment;
                review.IsEdited = true;
                review.UpdatedAt = DateTime.UtcNow;
                review.IsAiFlagged = isAiFlagged;
                review.AiFlagReason = aiFlagReason;

                if (newUploadedUrls.Count > 0)
                    review.ImageUrls = JsonSerializer.Serialize(newUploadedUrls);

                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                await RecalculateAverageRatingAsync(review.CarId);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                if (newUploadedUrls.Count > 0 && oldImageUrls.Count > 0)
                    await CleanupCloudinaryImagesAsync(oldImageUrls);

                return ServiceResult<ReviewDto>.Ok(MapToDto(review));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                if (newUploadedUrls.Count > 0)
                    await CleanupCloudinaryImagesAsync(newUploadedUrls);
                return ServiceResult<ReviewDto>.Fail("Lỗi khi cập nhật đánh giá. Vui lòng thử lại.");
            }
        }

        // ═══════════════════════════════════════════════════
        // TOGGLE VISIBILITY (admin soft-delete/restore)
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> ToggleReviewVisibilityAsync(int reviewId)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null)
                return ServiceResult.Fail("Không tìm thấy đánh giá.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                review.IsDeleted = !review.IsDeleted;
                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                await RecalculateAverageRatingAsync(review.CarId);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return ServiceResult.Ok();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail("Lỗi khi cập nhật trạng thái đánh giá.");
            }
        }

        // ═══════════════════════════════════════════════════
        // APPROVE AI-FLAGGED REVIEW (admin action)
        // Sets IsAiFlagged=false, recalculates AverageRating
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult> ApproveAiFlaggedReviewAsync(int reviewId)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null)
                return ServiceResult.Fail("Không tìm thấy đánh giá.");

            if (!review.IsAiFlagged)
                return ServiceResult.Fail("Đánh giá này không bị AI gắn cờ.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                review.IsAiFlagged = false;
                review.AiFlagReason = null;
                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                // Now include this review in AverageRating
                await RecalculateAverageRatingAsync(review.CarId);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return ServiceResult.Ok();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail("Lỗi khi phê duyệt đánh giá.");
            }
        }

        // ═══════════════════════════════════════════════════
        // GET AVERAGE RATING (excludes deleted AND AI-flagged)
        // ═══════════════════════════════════════════════════
        public async Task<double> GetAverageRatingAsync(int carId)
        {
            var reviews = await _unitOfWork.Reviews
                .FindAsync(r => r.CarId == carId && !r.IsDeleted && !r.IsAiFlagged);
            var reviewList = reviews.ToList();
            return reviewList.Count > 0 ? reviewList.Average(r => r.Rating) : 0;
        }

        // ═══════════════════════════════════════════════════
        // USER DELETE OWN REVIEW
        // ═══════════════════════════════════════════════════
        public async Task<bool> DeleteAsync(int reviewId, string userId)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null || review.UserId != userId) return false;

            review.IsActive = false;
            review.IsDeleted = true;
            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync();

            await RecalculateAverageRatingAsync(review.CarId);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // ═══════════════════════════════════════════════════
        // ADMIN REPLY TO REVIEW
        // ═══════════════════════════════════════════════════
        public async Task<ServiceResult<ReviewDto>> AdminReplyAsync(int reviewId, string adminReply)
        {
            var review = await _unitOfWork.Reviews.Query()
                .Include(r => r.User)
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                return ServiceResult<ReviewDto>.Fail("Không tìm thấy đánh giá.");

            review.AdminReply = adminReply.Trim();
            review.AdminRepliedAt = DateTime.UtcNow;
            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<ReviewDto>.Ok(MapToDto(review));
        }

        // ═══════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════

        private static string? ValidateImageFiles(List<IFormFile>? images)
        {
            if (images == null || images.Count == 0) return null;
            if (images.Count > MaxImageCount)
                return $"Chỉ được upload tối đa {MaxImageCount} ảnh.";

            foreach (var file in images)
            {
                if (file.Length > MaxFileSize)
                    return $"File '{file.FileName}' vượt quá 2MB.";
                var ext = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(ext))
                    return $"File '{file.FileName}' không hỗ trợ. Chỉ chấp nhận .jpg, .png.";
            }
            return null;
        }

        /// <summary>
        /// AverageRating = AVG of reviews where !IsDeleted AND !IsAiFlagged
        /// </summary>
        private async Task RecalculateAverageRatingAsync(int carId)
        {
            var car = await _unitOfWork.Cars.GetByIdAsync(carId);
            if (car == null) return;

            var activeReviews = await _unitOfWork.Reviews
                .FindAsync(r => r.CarId == carId && !r.IsDeleted && !r.IsAiFlagged);
            var list = activeReviews.ToList();

            car.AverageRating = list.Count > 0 ? list.Average(r => r.Rating) : 0;
            _unitOfWork.Cars.Update(car);
        }

        private async Task CleanupCloudinaryImagesAsync(List<string> imageUrls)
        {
            foreach (var url in imageUrls)
            {
                try
                {
                    var publicId = ExtractCloudinaryPublicId(url);
                    if (!string.IsNullOrEmpty(publicId))
                        await _cloudinaryService.DeleteImageAsync(publicId);
                }
                catch { }
            }
        }

        private static ReviewDto MapToDto(Review r)
        {
            List<string>? imageUrls = null;
            if (!string.IsNullOrEmpty(r.ImageUrls))
            {
                try { imageUrls = JsonSerializer.Deserialize<List<string>>(r.ImageUrls); }
                catch { }
            }

            return new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User?.FullName ?? "Ẩn danh",
                UserAvatar = r.User?.Avatar,
                CarId = r.CarId,
                CarName = r.Car?.Name ?? string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                ImageUrls = imageUrls,
                IsDeleted = r.IsDeleted,
                IsEdited = r.IsEdited,
                UpdatedAt = r.UpdatedAt,
                IsAiFlagged = r.IsAiFlagged,
                AiFlagReason = r.AiFlagReason,
                OrderItemId = r.OrderItemId,
                CreatedDate = r.CreatedDate,
                AdminReply = r.AdminReply,
                AdminRepliedAt = r.AdminRepliedAt
            };
        }

        private static string? ExtractCloudinaryPublicId(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var uploadIndex = path.IndexOf("/upload/", StringComparison.Ordinal);
                if (uploadIndex < 0) return null;

                var afterUpload = path[(uploadIndex + 8)..];
                var slashIndex = afterUpload.IndexOf('/');
                if (slashIndex < 0) return null;

                var publicIdWithExt = afterUpload[(slashIndex + 1)..];
                var dotIndex = publicIdWithExt.LastIndexOf('.');
                return dotIndex > 0 ? publicIdWithExt[..dotIndex] : publicIdWithExt;
            }
            catch { return null; }
        }
    }
}
