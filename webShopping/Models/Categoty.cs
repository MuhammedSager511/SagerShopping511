using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class Categoty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string NameAr { get; set; } = "";
    }
}
