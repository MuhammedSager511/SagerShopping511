namespace webShopping.Models
{
    public class ShippingSettings
    {
        public int Id { get; set; }

        /// <summary>Warehouse / store country for same-city free delivery.</summary>
        public string StoreCountry { get; set; } = "Syria";

        public string StoreCity { get; set; } = "Damascus";

        /// <summary>Free delivery when customer city matches store city.</summary>
        public bool FreeShippingSameCity { get; set; } = true;

        /// <summary>Order subtotal (USD) above which shipping is free. 0 = disabled.</summary>
        public decimal FreeShippingMinOrderUsd { get; set; }

        public decimal DefaultCostUsd { get; set; } = 18m;
        public decimal DefaultVatRate { get; set; }
        public int DefaultMinDays { get; set; } = 7;
        public int DefaultMaxDays { get; set; } = 21;
    }
}
