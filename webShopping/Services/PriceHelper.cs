namespace webShopping.Services
{
    public static class PriceHelper
    {
        public static double EffectiveUnitPrice(Models.Product p)
        {
            if (p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price)
                return p.SalePrice.Value;
            return p.Price;
        }

        public static bool HasDiscount(Models.Product p) =>
            p.SalePrice.HasValue && p.SalePrice.Value > 0 && p.SalePrice.Value < p.Price;

        public static int DiscountPercent(Models.Product p)
        {
            if (!HasDiscount(p) || p.Price <= 0) return 0;
            return (int)Math.Round((1 - (p.SalePrice!.Value / p.Price)) * 100);
        }
    }
}
