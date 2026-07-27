using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webShopping.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }

        public string ApplicationUserId { get; set; } = "";

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        public DateTime orderDate { get; set; }

        /// <summary>Total in USD (base catalog currency).</summary>
        public double orderTotal { get; set; }

        public string orderStatus { get; set; } = Diger.status_pending;

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string Addres { get; set; } = "";

        public string Semt { get; set; } = "";

        [Required]
        public string sehir { get; set; } = "";

        public string PostKodu { get; set; } = "";

        // Legacy card fields — no longer collected; kept for DB compatibility.
        public string CartName { get; set; } = "";
        public string CartNumber { get; set; } = "";
        public string ExpirationMonth { get; set; } = "";
        public string ExpiratioYear { get; set; } = "";
        public string CVC { get; set; } = "";

        /// <summary>Currency shown to customer at checkout (e.g. EUR, SYP).</summary>
        public string PaymentCurrency { get; set; } = "USD";

        /// <summary>USD → TRY rate at payment time.</summary>
        public double ExchangeRateToTry { get; set; }

        /// <summary>Amount charged in TRY via Iyzipay.</summary>
        public double TotalPaidTry { get; set; }

        public double SubtotalUsd { get; set; }
        public double ShippingUsd { get; set; }
        public double VatUsd { get; set; }
        public string Country { get; set; } = "Syria";

        public string? ConversationId { get; set; }
        public string? IyzipayToken { get; set; }
        public string? IyzipayPaymentId { get; set; }
        public string? PayPalOrderId { get; set; }
        public string PaymentMethod { get; set; } = Diger.Payment_BankTransfer;
        public string? PaymentReference { get; set; }
        public string? PaymentBankName { get; set; }
        public DateTime? PaymentSubmittedAt { get; set; }
        public string? TrackingNumber { get; set; }

        [NotMapped]
        public OrderDetailsVM? detailsVM { get; set; }

        public ICollection<orderDetails> orderDetails { get; set; } = new List<orderDetails>();
    }
}
