using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    public class TransactionHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [MaxLength(50)]
        public string? StatusFrom { get; set; }

        [MaxLength(50)]
        public string? StatusTo { get; set; }   

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(100)]
        public string? ChangedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
