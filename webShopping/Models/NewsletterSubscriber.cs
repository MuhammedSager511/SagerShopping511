using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class NewsletterSubscriber
    {
        public int Id { get; set; }

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = "";

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
