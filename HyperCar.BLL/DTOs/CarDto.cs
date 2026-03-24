namespace HyperCar.BLL.DTOs
{
    public class CarDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int HorsePower { get; set; }
        public string? Engine { get; set; }
        public decimal TopSpeed { get; set; }
        public decimal Acceleration { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
        public string? DescriptionVi { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageGallery { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class CarCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int BrandId { get; set; }
        public decimal Price { get; set; }
        public int HorsePower { get; set; }
        public string? Engine { get; set; }
        public decimal TopSpeed { get; set; }
        public decimal Acceleration { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
        public string? DescriptionVi { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageGallery { get; set; }
        public string? Category { get; set; }
    }

    public class CarFilterDto
    {
        public string? SearchTerm { get; set; }
        public int? BrandId { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinHorsePower { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
