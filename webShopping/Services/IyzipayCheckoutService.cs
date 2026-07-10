using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using System.Globalization;
using webShopping.Models;

namespace webShopping.Services
{
    public interface IIyzipayCheckoutService
    {
        bool IsConfigured { get; }
        CheckoutFormInitializeResult InitializeCheckout(OrderHeader order, IEnumerable<(string Name, string Category, double PriceUsd, int Qty)> items, string buyerEmail, string callbackUrl);
        CheckoutFormRetrieveResult? RetrievePayment(string token);
    }

    public record CheckoutFormInitializeResult(bool Success, string? PaymentPageUrl, string? Token, string? ErrorMessage);
    public record CheckoutFormRetrieveResult(bool Success, string? PaymentId, string? PaymentStatus, string? ErrorMessage);

    public class IyzipayCheckoutService : IIyzipayCheckoutService
    {
        private readonly IConfiguration _configuration;
        private readonly ICurrencyService _currency;

        public IyzipayCheckoutService(IConfiguration configuration, ICurrencyService currency)
        {
            _configuration = configuration;
            _currency = currency;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["Iyzipay:ApiKey"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Iyzipay:SecretKey"]);

        public CheckoutFormInitializeResult InitializeCheckout(
            OrderHeader order,
            IEnumerable<(string Name, string Category, double PriceUsd, int Qty)> items,
            string buyerEmail,
            string callbackUrl)
        {
            if (!IsConfigured)
                return new CheckoutFormInitializeResult(false, null, null, "Iyzipay API keys are not configured.");

            if (order.TotalPaidTry <= 0)
                return new CheckoutFormInitializeResult(false, null, null, "Order total must be greater than zero.");

            var options = GetOptions();
            var culture = CultureInfo.InvariantCulture;
            var totalTry = (decimal)order.TotalPaidTry;
            var priceStr = totalTry.ToString("F2", culture);

            var basketItems = BuildBasketItems(order, items, culture, totalTry);

            var request = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = order.ConversationId ?? order.Id.ToString(),
                Price = priceStr,
                PaidPrice = priceStr,
                Currency = Currency.TRY.ToString(),
                BasketId = order.Id.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = callbackUrl,
                EnabledInstallments = new List<int> { 1 },
                Buyer = new Buyer
                {
                    Id = order.ApplicationUserId,
                    Name = order.Name,
                    Surname = order.LastName,
                    GsmNumber = NormalizePhone(order.Country, order.PhoneNumber),
                    Email = buyerEmail,
                    IdentityNumber = "11111111111",
                    RegistrationAddress = order.Addres,
                    City = order.sehir,
                    Country = MapCountry(order.Country),
                    ZipCode = string.IsNullOrWhiteSpace(order.PostKodu) ? "34000" : order.PostKodu
                },
                ShippingAddress = new Address
                {
                    ContactName = $"{order.Name} {order.LastName}",
                    City = order.sehir,
                    Country = MapCountry(order.Country),
                    Description = order.Addres,
                    ZipCode = string.IsNullOrWhiteSpace(order.PostKodu) ? "34000" : order.PostKodu
                },
                BillingAddress = new Address
                {
                    ContactName = $"{order.Name} {order.LastName}",
                    City = order.sehir,
                    Country = MapCountry(order.Country),
                    Description = order.Addres,
                    ZipCode = string.IsNullOrWhiteSpace(order.PostKodu) ? "34000" : order.PostKodu
                }
            };

            request.BasketItems = basketItems;

            var result = CheckoutFormInitialize.Create(request, options);
            if (result.Status == Status.SUCCESS.ToString())
                return new CheckoutFormInitializeResult(true, result.PaymentPageUrl, result.Token, null);

            var error = result.ErrorMessage;
            if (!string.IsNullOrWhiteSpace(result.ErrorCode))
                error = $"{result.ErrorCode}: {error}";

            return new CheckoutFormInitializeResult(false, null, null, error);
        }

        public CheckoutFormRetrieveResult? RetrievePayment(string token)
        {
            var options = GetOptions();
            var request = new RetrieveCheckoutFormRequest { Token = token };
            var result = CheckoutForm.Retrieve(request, options);

            if (result.Status == Status.SUCCESS.ToString() && result.PaymentStatus == "SUCCESS")
                return new CheckoutFormRetrieveResult(true, result.PaymentId, result.PaymentStatus, null);

            return new CheckoutFormRetrieveResult(false, result.PaymentId, result.PaymentStatus, result.ErrorMessage);
        }

        private List<BasketItem> BuildBasketItems(
            OrderHeader order,
            IEnumerable<(string Name, string Category, double PriceUsd, int Qty)> items,
            CultureInfo culture,
            decimal totalTry)
        {
            var basketItems = new List<BasketItem>();
            var index = 1;

            foreach (var item in items)
            {
                var lineTry = (decimal)_currency.ConvertFromUsd(item.PriceUsd * item.Qty, "TRY");
                basketItems.Add(CreateBasketItem(index++, item.Name, item.Category, lineTry, culture));
            }

            if (order.ShippingUsd > 0)
            {
                var shipTry = (decimal)_currency.ConvertFromUsd(order.ShippingUsd, "TRY");
                basketItems.Add(CreateBasketItem(index++, "Shipping", "Shipping", shipTry, culture));
            }

            if (order.VatUsd > 0)
            {
                var vatTry = (decimal)_currency.ConvertFromUsd(order.VatUsd, "TRY");
                basketItems.Add(CreateBasketItem(index, "VAT", "Tax", vatTry, culture, BasketItemType.VIRTUAL));
            }

            AdjustBasketTotal(basketItems, totalTry, culture);
            return basketItems;
        }

        private static BasketItem CreateBasketItem(
            int id,
            string name,
            string category,
            decimal priceTry,
            CultureInfo culture,
            BasketItemType type = BasketItemType.PHYSICAL) =>
            new()
            {
                Id = id.ToString(),
                Name = Truncate(name, 100),
                Category1 = string.IsNullOrWhiteSpace(category) ? "General" : Truncate(category, 50),
                ItemType = type.ToString(),
                Price = priceTry.ToString("F2", culture)
            };

        private static void AdjustBasketTotal(List<BasketItem> basketItems, decimal totalTry, CultureInfo culture)
        {
            if (basketItems.Count == 0)
                return;

            var sum = basketItems.Sum(i => decimal.Parse(i.Price, culture));
            var diff = totalTry - sum;

            if (Math.Abs(diff) < 0.01m)
                return;

            var last = basketItems[^1];
            var adjusted = decimal.Parse(last.Price, culture) + diff;
            if (adjusted < 0)
                adjusted = 0;

            last.Price = adjusted.ToString("F2", culture);
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];

        private static string MapCountry(string country) =>
            country.Trim() switch
            {
                "TR" => "Turkey",
                "SY" => "Syria",
                "US" => "United States",
                "GB" => "United Kingdom",
                "DE" => "Germany",
                _ => country.Trim()
            };

        private static string NormalizePhone(string country, string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits))
                return "+905551234567";

            if (phone.TrimStart().StartsWith('+'))
                return phone.Trim();

            var c = country.Trim();
            if (c.Equals("Turkey", StringComparison.OrdinalIgnoreCase) || c.Equals("TR", StringComparison.OrdinalIgnoreCase))
                return $"+90{digits.TrimStart('0')}";

            if (c.Equals("Syria", StringComparison.OrdinalIgnoreCase) || c.Equals("SY", StringComparison.OrdinalIgnoreCase))
                return $"+963{digits.TrimStart('0')}";

            if (c.Equals("United States", StringComparison.OrdinalIgnoreCase) || c.Equals("US", StringComparison.OrdinalIgnoreCase))
                return $"+1{digits}";

            if (c.Equals("United Kingdom", StringComparison.OrdinalIgnoreCase) || c.Equals("GB", StringComparison.OrdinalIgnoreCase))
                return $"+44{digits.TrimStart('0')}";

            if (c.Equals("Germany", StringComparison.OrdinalIgnoreCase) || c.Equals("DE", StringComparison.OrdinalIgnoreCase))
                return $"+49{digits.TrimStart('0')}";

            return digits.StartsWith("00") ? $"+{digits[2..]}" : $"+{digits}";
        }

        private Options GetOptions() => new()
        {
            ApiKey = _configuration["Iyzipay:ApiKey"],
            SecretKey = _configuration["Iyzipay:SecretKey"],
            BaseUrl = _configuration["Iyzipay:BaseUrl"] ?? "https://sandbox-api.iyzipay.com"
        };
    }
}
