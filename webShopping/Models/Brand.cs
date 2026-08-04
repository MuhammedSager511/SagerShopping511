using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webShopping.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [MaxLength(120)]
        public string NameAr { get; set; } = "";

        public string ImagePath { get; set; } = "";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public IFormFile? Image { get; set; }
    }
}
