using Microsoft.Extensions.Caching.Memory;

namespace webShopping.Services
{
    public static class CatalogCache
    {
        public const string HomeShowcase = "home_showcase_v4";

        public static void Invalidate(IMemoryCache cache)
        {
            cache.Remove(HomeShowcase);
            // Shop uses many dynamic keys; compact store via marker version bump.
            var version = cache.GetOrCreate("shop_cache_version", e =>
            {
                e.Priority = CacheItemPriority.NeverRemove;
                return 0;
            });
            cache.Set("shop_cache_version", version + 1);
        }

        public static string ShopKey(int page, int pageSize, string sort, int? categoryId, string? q)
        {
            // version is read by caller via GetShopVersion
            return $"shop_p{page}_s{pageSize}_{sort}_c{categoryId}_q{q ?? ""}";
        }

        public static int GetShopVersion(IMemoryCache cache) =>
            cache.Get<int?>("shop_cache_version") ?? 0;
    }
}
