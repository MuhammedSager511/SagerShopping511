using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webShopping.Models
{
    public class orderDetails
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int OrederId { get; set; }
        [ForeignKey("OrederId")]
        public OrderHeader OrderHeader { get; set; }
        public int productId { get; set; }
        [ForeignKey("productId")]
        public Product product { get; set; }
        public int count { get; set; }
        public double Price { get; set; }
    }
}
