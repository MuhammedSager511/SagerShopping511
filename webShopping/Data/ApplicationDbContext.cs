using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using webShopping.Models;

namespace webShopping.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ShippingSettings>(e =>
            {
                e.Property(s => s.DefaultCostUsd).HasPrecision(10, 2);
                e.Property(s => s.DefaultVatRate).HasPrecision(5, 4);
                e.Property(s => s.FreeShippingMinOrderUsd).HasPrecision(10, 2);
            });

            builder.Entity<ShippingZone>(e =>
            {
                e.Property(z => z.ShippingCostUsd).HasPrecision(10, 2);
                e.Property(z => z.VatRate).HasPrecision(5, 4);
            });
        }
        public DbSet<Categoty> Categoties { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<orderDetails> orderDetailses { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<QuickLink> QuickLinks { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ShippingSettings> ShippingSettings { get; set; }
        public DbSet<ShippingZone> ShippingZones { get; set; }
        public DbSet<AppNotification> AppNotifications { get; set; }
    }
}
