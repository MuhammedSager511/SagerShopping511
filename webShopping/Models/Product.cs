

using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations.Schema;
using webShopping.Models;

namespace webShopping.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
      
        public string? Path { get; set; }
        [Required]
        public double Price { get; set; }
        [Required]
        public string? FileType { get; set; }
     
        public string Description { get; set; }

        public bool IsHome { get; set; }
        public bool IsStock { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Categoty categoty { get; set; }
        [Required]
        [NotMapped]
       
        public IFormFile? File { get; set; }
       

    }

}
