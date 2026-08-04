using webShopping.Models;

namespace webShopping.Services
{
    public enum StockLevel
    {
        OutOfStock,
        Limited,
        InStock
    }

    public static class StockHelper
    {
        public const int LimitedThreshold = 5;

        public static StockLevel GetLevel(Product? product)
        {
            if (product == null || product.StockQuantity <= 0 || !product.IsStock)
                return StockLevel.OutOfStock;
            if (product.StockQuantity <= LimitedThreshold)
                return StockLevel.Limited;
            return StockLevel.InStock;
        }

        public static string BadgeCss(StockLevel level) => level switch
        {
            StockLevel.OutOfStock => "stock-out",
            StockLevel.Limited => "stock-limited",
            _ => "stock-in"
        };

        public static string Label(Product? product, IAppLocalizer loc)
        {
            var level = GetLevel(product);
            return level switch
            {
                StockLevel.OutOfStock => loc["OutOfStock"],
                StockLevel.Limited => string.Format(loc["OnlyXLeft"], product!.StockQuantity),
                _ => loc["InStock"]
            };
        }

        public static bool CanFulfill(Product product, int requestedQty) =>
            product.StockQuantity >= requestedQty && requestedQty > 0;
    }
}
