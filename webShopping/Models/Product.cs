using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webShopping.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string NameAr { get; set; } = "";
        public string DescriptionAr { get; set; } = "";

        public string? Path { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public double Price { get; set; }

        /// <summary>Optional discounted price in USD. When set and lower than Price, it is the selling price.</summary>
        [Range(0.01, double.MaxValue)]
        public double? SalePrice { get; set; }

        /// <summary>Comma-separated color options, e.g. Red,Blue,Black</summary>
        [MaxLength(500)]
        public string Colors { get; set; } = "";

        /// <summary>Comma-separated size options, e.g. S,M,L,XL</summary>
        [MaxLength(500)]
        public string Sizes { get; set; } = "";

        public int? BrandId { get; set; }

        [ForeignKey("BrandId")]
        public Brand? Brand { get; set; }

        public string? FileType { get; set; }

        public string Description { get; set; } = "";

        public bool IsHome { get; set; }

        /// <summary>True when StockQuantity &gt; 0. Kept for existing queries/UI.</summary>
        public bool IsStock { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } = 0;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Categoty? categoty { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        [NotMapped]
        public IFormFile? File { get; set; }

        [NotMapped]
        public List<IFormFile>? GalleryFiles { get; set; }

        public void SyncStockFlag()
        {
            if (StockQuantity < 0) StockQuantity = 0;
            IsStock = StockQuantity > 0;
        }
    }
}
