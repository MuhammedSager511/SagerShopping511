namespace webShopping.Services
{
    public static class OrderStatusHelper
    {
        /// <summary>
        /// Customer-facing progress: معلقة → تأكيد الدفع → تأكيد الطلب → شحن
        /// </summary>
        public static readonly string[] ProgressSteps =
        {
            Models.Diger.status_awaiting_payment,
            Models.Diger.status_payment_review,
            Models.Diger.status_confirmed,
            Models.Diger.status_cargo
        };

        public static string Label(IAppLocalizer loc, string? status) => Label(loc, loc.CurrentLanguage, status);

        public static string Label(IAppLocalizer loc, string culture, string? status) => status switch
        {
            Models.Diger.status_pending => loc.Get("OrderStagePending", culture),
            Models.Diger.status_awaiting_payment => loc.Get("OrderStagePending", culture),
            Models.Diger.status_payment_review => loc.Get("OrderStagePaymentConfirm", culture),
            Models.Diger.status_confirmed => loc.Get("OrderStageConfirmed", culture),
            // Legacy "preparing" is treated as confirmed
            Models.Diger.status_preparing => loc.Get("OrderStageConfirmed", culture),
            Models.Diger.status_cargo => loc.Get("OrderStageShipped", culture),
            Models.Diger.status_cancelled => loc.Get("Cancelled", culture),
            _ => status ?? ""
        };

        public static string BadgeClass(string? status) => status switch
        {
            Models.Diger.status_pending => "bg-warning text-dark",
            Models.Diger.status_awaiting_payment => "bg-warning text-dark",
            Models.Diger.status_payment_review => "bg-primary",
            Models.Diger.status_confirmed => "bg-success",
            Models.Diger.status_preparing => "bg-success",
            Models.Diger.status_cargo => "bg-info text-dark",
            Models.Diger.status_cancelled => "bg-secondary",
            _ => "bg-secondary"
        };

        /// <summary>
        /// Progress index 0..3 for track UI; -1 if cancelled/unknown.
        /// </summary>
        public static int ProgressIndex(string? status)
        {
            return status switch
            {
                Models.Diger.status_pending => 0,
                Models.Diger.status_awaiting_payment => 0,
                Models.Diger.status_payment_review => 1,
                Models.Diger.status_confirmed => 2,
                Models.Diger.status_preparing => 2, // legacy mid-step → order confirmed
                Models.Diger.status_cargo => 3,
                _ => -1
            };
        }

        public static bool CanShip(string? status) =>
            status is Models.Diger.status_confirmed or Models.Diger.status_preparing;

        public static bool CanVerifyPayment(string? status) =>
            status is Models.Diger.status_payment_review or Models.Diger.status_pending;
    }
}
