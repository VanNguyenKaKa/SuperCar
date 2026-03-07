namespace HyperCar.BLL.DTOs
{
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Logo { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int CarCount { get; set; }
    }
}
