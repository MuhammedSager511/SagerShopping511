using System.Collections.Concurrent;
using webShopping.Models;

namespace webShopping
{
    public static class RecentErrors
    {
        private static readonly ConcurrentDictionary<string, (DateTime At, ErrorViewModel Error)> StoreMap = new();

        public static void Store(string requestId, ErrorViewModel error)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;
            StoreMap[requestId] = (DateTime.UtcNow, error);
            Cleanup();
        }

        public static ErrorViewModel? Get(string? requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return null;
            return StoreMap.TryGetValue(requestId, out var item) ? item.Error : null;
        }

        private static void Cleanup()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            foreach (var kv in StoreMap)
            {
                if (kv.Value.At < cutoff)
                    StoreMap.TryRemove(kv.Key, out _);
            }
        }
    }
}
