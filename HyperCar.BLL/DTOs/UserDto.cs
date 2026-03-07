namespace HyperCar.BLL.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
