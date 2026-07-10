using webShopping.Models;

namespace webShopping.Services
{
    public static class NotificationCulture
    {
        public static string FromCountry(string? country)
        {
            if (string.IsNullOrWhiteSpace(country)) return "en";
            var c = country.Trim();
            return c.Equals("Syria", StringComparison.OrdinalIgnoreCase)
                || c.Equals("SY", StringComparison.OrdinalIgnoreCase)
                || c.Contains("سور", StringComparison.Ordinal)
                ? "ar" : "en";
        }
    }

    public static class OrderEmailBuilder
    {
        public static string ConfirmedSubject(IAppLocalizer loc, string culture, OrderHeader order) =>
            loc.Get("EmailOrderConfirmedSubject", culture).Replace("{OrderId}", order.Id.ToString());

        public static string PlacedSubject(IAppLocalizer loc, string culture, OrderHeader order) =>
            loc.Get("EmailOrderPlacedSubject", culture).Replace("{OrderId}", order.Id.ToString());

        public static string ShippedSubject(IAppLocalizer loc, string culture, OrderHeader order) =>
            loc.Get("EmailOrderShippedSubject", culture).Replace("{OrderId}", order.Id.ToString());

        public static string ConfirmedBody(IAppLocalizer loc, string culture, OrderHeader order)
        {
            var status = OrderStatusHelper.Label(loc, culture, order.orderStatus);
            var template = loc.Get("EmailOrderConfirmedBody", culture);
            var paymentDetail = order.PaymentMethod == Diger.Payment_PayPal
                ? loc.Get("EmailPaidViaPayPal", culture).Replace("{Total}", order.orderTotal.ToString("N2"))
                : order.PaymentMethod == Diger.Payment_BankTransfer
                    ? loc.Get("EmailPaidViaBankTransfer", culture)
                    : loc.Get("EmailPaidViaIyzipay", culture)
                        .Replace("{Try}", order.TotalPaidTry.ToString("N2"))
                        .Replace("{Currency}", order.PaymentCurrency)
                        .Replace("{Rate}", order.ExchangeRateToTry.ToString("N4"));

            return template
                .Replace("{OrderId}", order.Id.ToString())
                .Replace("{Status}", status)
                .Replace("{Total}", order.orderTotal.ToString("N2"))
                .Replace("{PaymentDetail}", paymentDetail);
        }

        public static string PlacedBody(IAppLocalizer loc, string culture, OrderHeader order, string bankInfo)
        {
            var template = loc.Get("EmailOrderPlacedBody", culture);
            return template
                .Replace("{OrderId}", order.Id.ToString())
                .Replace("{Total}", order.orderTotal.ToString("N2"))
                .Replace("{BankInfo}", bankInfo.Replace("\n", "<br/>"));
        }

        public static string ShippedBody(IAppLocalizer loc, string culture, OrderHeader order)
        {
            var template = loc.Get("EmailOrderShippedBody", culture);
            return template
                .Replace("{OrderId}", order.Id.ToString())
                .Replace("{Tracking}", order.TrackingNumber ?? "—")
                .Replace("{Name}", $"{order.Name} {order.LastName}".Trim());
        }
    }
}
