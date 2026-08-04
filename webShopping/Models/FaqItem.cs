using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class FaqItem
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string QuestionEn { get; set; } = "";

        [MaxLength(300)]
        public string QuestionAr { get; set; } = "";

        [Required]
        public string AnswerEn { get; set; } = "";

        public string AnswerAr { get; set; } = "";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
