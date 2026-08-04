using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string BankNameEn { get; set; } = "";

        [Required, MaxLength(120)]
        public string BankNameAr { get; set; } = "";

        [Required, MaxLength(80)]
        public string AccountNumber { get; set; } = "";

        [MaxLength(80)]
        public string? Iban { get; set; }

        [MaxLength(120)]
        public string BeneficiaryEn { get; set; } = "SagerShop";

        [MaxLength(120)]
        public string BeneficiaryAr { get; set; } = "SagerShop";

        [MaxLength(300)]
        public string? NotesEn { get; set; }

        [MaxLength(300)]
        public string? NotesAr { get; set; }

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
