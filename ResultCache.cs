using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base;
public static class ResultCache {
    private static MemoryCache __Cache { get; } = new MemoryCache(new MemoryCacheOptions {
        ExpirationScanFrequency = TimeSpan.FromSeconds(30)
    });

    public static bool Set(int hashCode, ResultCacheRow row) {
        if (row == null) {
            return false;
        }

        return __Cache.Set(
            hashCode, 
            row, 
            new MemoryCacheEntryOptions()
                .SetSlidingExpiration (TimeSpan.FromMinutes(5)) // resets after each access
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)) // max total lifetime
        ) != null;
    }

    public static ResultCacheRow Get(int hashCode) {
        if (__Cache.TryGetValue(hashCode, out ResultCacheRow cached)) {
            return cached;
        }

        return null;
    }
}
