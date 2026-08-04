using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required, MaxLength(40)]
        public string Code { get; set; } = "";

        [MaxLength(120)]
        public string TitleEn { get; set; } = "";

        [MaxLength(120)]
        public string TitleAr { get; set; } = "";

        /// <summary>Percent 1-100, or fixed USD amount when IsPercent=false.</summary>
        [Range(0.01, 100000)]
        public double Value { get; set; }

        public bool IsPercent { get; set; } = true;

        public double MinOrderUsd { get; set; }
        public int MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
