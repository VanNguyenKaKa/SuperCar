using System.ComponentModel.DataAnnotations;

namespace HyperCar.DAL.Entities
{
    public class Brand
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(500)]
        public string? Logo { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
    }
}
