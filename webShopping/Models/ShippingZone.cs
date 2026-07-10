using System.ComponentModel.DataAnnotations;

namespace webShopping.Models
{
    public class ShippingZone
    {
        public int Id { get; set; }

        [Required]
        public string Country { get; set; } = "";

        /// <summary>Leave empty for country-wide default zone.</summary>
        public string? City { get; set; }

        public decimal ShippingCostUsd { get; set; }
        public decimal VatRate { get; set; }
        public int DeliveryMinDays { get; set; } = 3;
        public int DeliveryMaxDays { get; set; } = 10;
        public bool IsFreeShipping { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
