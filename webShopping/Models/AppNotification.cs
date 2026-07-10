namespace webShopping.Models
{
    public class AppNotification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string TitleAr { get; set; } = "";
        public string MessageEn { get; set; } = "";
        public string MessageAr { get; set; } = "";
        public string? LinkUrl { get; set; }
        public string Type { get; set; } = "info";
        public int? OrderId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
