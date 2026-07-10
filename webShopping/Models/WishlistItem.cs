namespace webShopping.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
