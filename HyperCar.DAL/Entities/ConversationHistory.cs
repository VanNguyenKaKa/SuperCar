using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperCar.DAL.Entities
{
    
    public class ConversationHistory
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [MaxLength(200)]
        public string? SessionId { get; set; }

        [Required]
        [MaxLength(4000)]
        public string UserMessage { get; set; } = string.Empty;

        [Required]
        [MaxLength(8000)]
        public string AiResponse { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }
}
