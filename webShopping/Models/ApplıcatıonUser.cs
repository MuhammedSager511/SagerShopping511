using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations.Schema;

namespace webShopping.Models
{
    public class ApplicationUser:IdentityUser
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
        
        public string Addres { get; set; }

        public string City { get; set; }
        public string Country { get; set; }
        public string PostaKodu { get; set; }
        [NotMapped]
        public string Role { get; set; }

    }
}
