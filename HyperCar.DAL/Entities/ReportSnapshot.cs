using System.ComponentModel.DataAnnotations;

namespace HyperCar.DAL.Entities
{
    public class ReportSnapshot
    {
        public int Id { get; set; }

        public DateTime ReportDate { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        public string? JsonData { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
