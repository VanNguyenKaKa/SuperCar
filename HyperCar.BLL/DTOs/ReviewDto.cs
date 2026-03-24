using Microsoft.AspNetCore.Http;

namespace HyperCar.BLL.DTOs
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<string>? ImageUrls { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsAiFlagged { get; set; }
        public string? AiFlagReason { get; set; }
        public int? OrderItemId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AdminReply { get; set; }
        public DateTime? AdminRepliedAt { get; set; }
    }

    public class CreateReviewDto
    {
        public int CarId { get; set; }
        public int OrderItemId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class EditReviewDto
    {
        public int ReviewId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
