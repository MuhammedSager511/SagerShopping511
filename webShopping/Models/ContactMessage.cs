using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = "";

        [MaxLength(200)]
        public string Subject { get; set; } = "";

        [Required, MaxLength(2000)]
        public string Message { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}
