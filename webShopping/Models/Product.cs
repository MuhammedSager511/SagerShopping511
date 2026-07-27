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

        public string? FileType { get; set; }

        public string Description { get; set; } = "";

        public bool IsHome { get; set; }
        public bool IsStock { get; set; } = true;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Categoty? categoty { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        [NotMapped]
        public IFormFile? File { get; set; }

        [NotMapped]
        public List<IFormFile>? GalleryFiles { get; set; }
    }
}
