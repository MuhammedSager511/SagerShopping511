using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }
        public DateTime orderDate { get; set; }
        public double orderTotal { get; set; }
        public string orderStatus { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Addres { get; set; }
        [Required]
        public string Semt { get; set; }
        [Required]
        public string sehir { get; set; }
        [Required]
        public string PostKodu { get; set; }
        [Required]
        public string CartName { get; set; }
        [Required]
        public string CartNumber { get; set; }
        [Required]
        public string ExpirationMonth { get; set; }
        [Required]
        public string ExpiratioYear{ get; set; }
        [Required]
        public string CVC { get; set; }
    }
}
