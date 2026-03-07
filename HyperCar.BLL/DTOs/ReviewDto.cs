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
        public DateTime CreatedDate { get; set; }
    }

    public class CreateReviewDto
    {
        public int CarId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
