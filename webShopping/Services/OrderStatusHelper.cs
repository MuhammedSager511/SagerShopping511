namespace webShopping.Services
{
    public static class OrderStatusHelper
    {
        public static string Label(IAppLocalizer loc, string? status) => Label(loc, loc.CurrentLanguage, status);

        public static string Label(IAppLocalizer loc, string culture, string? status) => status switch
        {
            Models.Diger.status_pending => loc.Get("Pending", culture),
            Models.Diger.status_awaiting_payment => loc.Get("AwaitingPayment", culture),
            Models.Diger.status_payment_review => loc.Get("PaymentUnderReview", culture),
            Models.Diger.status_confirmed => loc.Get("Confirmed", culture),
            Models.Diger.status_cargo => loc.Get("Shipped", culture),
            _ => status ?? ""
        };

        public static string BadgeClass(string? status) => status switch
        {
            Models.Diger.status_pending => "bg-warning text-dark",
            Models.Diger.status_awaiting_payment => "bg-warning text-dark",
            Models.Diger.status_payment_review => "bg-primary",
            Models.Diger.status_confirmed => "bg-success",
            Models.Diger.status_cargo => "bg-info text-dark",
            _ => "bg-secondary"
        };
    }
}
