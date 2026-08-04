using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace webShopping.Models
{
    public class Categoty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string NameAr { get; set; } = "";

        public string ImagePath { get; set; } = "";

        public int SortOrder { get; set; }

        [NotMapped]
        public IFormFile? Image { get; set; }
    }
}
