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

    public static TimeSpan SlidingExpiration  { get; private set; } = TimeSpan.FromMinutes(5);
    public static TimeSpan AbsoluteExpiration { get; private set; } = TimeSpan.FromMinutes(10);

    public static void SetSlidingExpiration(TimeSpan timeSpan) {
        SlidingExpiration = timeSpan;
    }

    public static void SetAbsoluteExpiration(TimeSpan timeSpan) {
        AbsoluteExpiration = timeSpan;
    }

    public static bool Set(object row) {
        if (row != null) {
            return Set(row.GetHashCode(), new ResultCacheRow(row));
        }

        return false;
    }

    public static bool Set(int hashCode, ResultCacheRow row) {
        if (row == null) {
            return false;
        }

        return __Cache.Set(
            hashCode, 
            row, 
            new MemoryCacheEntryOptions()
                .SetSlidingExpiration (SlidingExpiration)  // resets after each access
                .SetAbsoluteExpiration(AbsoluteExpiration) // max total lifetime
        ) != null;
    }

    public static ResultCacheRow Get(int hashCode) {
        if (__Cache.TryGetValue(hashCode, out ResultCacheRow cached)) {
            return cached;
        }

        return null;
    }
}
