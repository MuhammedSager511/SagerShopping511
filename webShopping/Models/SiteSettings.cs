namespace webShopping.Models
{
    public class SiteSettings
    {
        public int Id { get; set; }

        public string HeroTitleEn { get; set; } = "Discover Premium Products Worldwide";
        public string HeroTitleAr { get; set; } = "اكتشف منتجات مميزة من حول العالم";
        public string HeroSubtitleEn { get; set; } = "Shop with confidence — fast delivery, secure checkout, multi-currency support.";
        public string HeroSubtitleAr { get; set; } = "تسوق بثقة — توصيل سريع، دفع آمن، ودعم عملات متعددة.";

        public string AboutTitleEn { get; set; } = "About SagerShop";
        public string AboutTitleAr { get; set; } = "من نحن";
        public string AboutBodyEn { get; set; } = "SagerShop is a global e-commerce platform offering quality products with secure checkout and fast delivery.";
        public string AboutBodyAr { get; set; } = "ساجر شوب منصة تجارة إلكترونية عالمية تقدم منتجات عالية الجودة مع دفع آمن وتوصيل سريع.";

        public string FooterTextEn { get; set; } = "Your trusted global online store.";
        public string FooterTextAr { get; set; } = "متجرك الإلكتروني العالمي الموثوق.";

        public string ContactEmail { get; set; } = "support@sagershop.com";
        public string ContactPhone { get; set; } = "+963 999 000 000";
        public string ContactAddressEn { get; set; } = "Damascus, Syria";
        public string ContactAddressAr { get; set; } = "دمشق، سوريا";
        public string ContactHoursEn { get; set; } = "24/7 Support";
        public string ContactHoursAr { get; set; } = "دعم على مدار الساعة";

        public string FacebookUrl { get; set; } = "#";
        public string InstagramUrl { get; set; } = "#";
        public string TwitterUrl { get; set; } = "#";
        public string WhatsAppUrl { get; set; } = "#";

        public string TermsTitleEn { get; set; } = "Terms of Use";
        public string TermsTitleAr { get; set; } = "شروط الاستخدام";
        public string TermsBodyEn { get; set; } = "By using SagerShop you agree to our terms of service, acceptable use, and payment policies.";
        public string TermsBodyAr { get; set; } = "باستخدامك ساجر شوب فإنك توافق على شروط الخدمة وسياسات الدفع.";

        public string RefundTitleEn { get; set; } = "Refund Policy";
        public string RefundTitleAr { get; set; } = "سياسة الاسترجاع";
        public string RefundBodyEn { get; set; } = "You may request a refund within 14 days for unused items in original packaging. Contact support@sagershop.com.";
        public string RefundBodyAr { get; set; } = "يمكنك طلب استرجاع خلال 14 يوماً للمنتجات غير المستخدمة وبعبوتها الأصلية. تواصل مع الدعم.";

        public string PrivacyTitleEn { get; set; } = "Privacy Policy";
        public string PrivacyTitleAr { get; set; } = "سياسة الخصوصية";
        public string PrivacyBodyEn { get; set; } = "We respect your privacy. We collect only data needed to process orders and improve our service. We never store payment card details on our servers. Contact us for data requests.";
        public string PrivacyBodyAr { get; set; } = "نحترم خصوصيتك. نجمع فقط البيانات اللازمة لمعالجة الطلبات وتحسين الخدمة. لا نخزّن بيانات البطاقة على خوادمنا. تواصل معنا لطلبات البيانات.";

        public string BankTransferInfoEn { get; set; } = "Sham Bank (Cash):\nAccount: 0000000000\nBeneficiary: SagerShop\n\nBEMO Bank:\nAccount: 0000000000\nIBAN: SY0000000000000000000000\nBeneficiary: SagerShop\n\nPlease include your order number in the transfer reference.";
        public string BankTransferInfoAr { get; set; } = "بنك الشام (كاش):\nرقم الحساب: 0000000000\nاسم المستفيد: SagerShop\n\nبنك بيمو:\nرقم الحساب: 0000000000\nIBAN: SY0000000000000000000000\nاسم المستفيد: SagerShop\n\nيرجى ذكر رقم الطلب في ملاحظات التحويل.";
    }
}
