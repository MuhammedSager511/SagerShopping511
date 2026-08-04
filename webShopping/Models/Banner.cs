using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webShopping.Models
{
    public class Banner
    {
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string TitleEn { get; set; } = "";

        [MaxLength(160)]
        public string TitleAr { get; set; } = "";

        [MaxLength(300)]
        public string SubtitleEn { get; set; } = "";

        [MaxLength(300)]
        public string SubtitleAr { get; set; } = "";

        public string ImagePath { get; set; } = "";
        public string LinkUrl { get; set; } = "/Home/Shop";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public IFormFile? Image { get; set; }
    }
}
