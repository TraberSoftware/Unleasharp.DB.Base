using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unleasharp.DB.Base.ExtensionMethods;

namespace Unleasharp.DB.Base;
/// <summary>
/// Provides a thread-safe cache for resolved table and column names obtained via reflection/extension methods.
/// </summary>
/// <remarks>
/// This static helper caches names to avoid repeated reflection costs. It is safe for concurrent use:
/// locks guard internal dictionaries when reading/writing cached values.
/// </remarks>
public static class ReflectionCache {
    #region Internal Structures
    /// <summary>
    /// Lock object used to synchronize access to the table name cache.
    /// </summary>
    private static object __TableNameCacheLock  = new object { };

    /// <summary>
    /// Lock object used to synchronize access to the column name cache.
    /// </summary>
    private static object __ColumnNameCacheLock = new object { };

    /// <summary>
    /// Cache storing resolved table names. Key is produced by <see cref="__GetTableCacheKeyName{T}"/>
    /// </summary>
    private static Dictionary<string, string> __TableNameCache  = new Dictionary<string, string>();

    /// <summary>
    /// Cache storing resolved column names. Key is produced by <see cref="__GetColumnCacheKeyName{T}"/>
    /// </summary>
    private static Dictionary<string, string> __ColumnNameCache = new Dictionary<string, string>();

    /// <summary>
    /// Build the cache key used to store the table name for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A string key unique to <typeparamref name="T"/> used in <see cref="__TableNameCache"/>.</returns>
    private static string __GetTableCacheKeyName<T>() {
        return __GetTableCacheKeyName(typeof(T));
    }

    /// <summary>
    /// Build the cache key used to store the table name for the provided <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Entity <see cref="Type"/>. Must not be <see langword="null"/>.</param>
    /// <returns>A string key unique to <paramref name="type"/> used in <see cref="__TableNameCache"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    private static string __GetTableCacheKeyName(Type type) {
        return $"<{type.Name}>";
    }

    /// <summary>
    /// Build the cache key used to store the column name for a given member of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="memberName">The member (property/field) name.</param>
    /// <returns>A string key unique to the member of <typeparamref name="T"/> used in <see cref="__ColumnNameCache"/>.</returns>
    private static string __GetColumnCacheKeyName<T>(string memberName) where T : class {
        return $"<{(__GetTableCacheKeyName<T>())}>.{memberName}";
    }

    /// <summary>
    /// Build the cache key used to store the column name for the provided <paramref name="type"/> and <paramref name="memberName"/>.
    /// </summary>
    /// <param name="type">Entity <see cref="Type"/>. Must not be <see langword="null"/>.</param>
    /// <param name="memberName">Member (property/field) name.</param>
    /// <returns>A string key unique to the provided type and member used in <see cref="__ColumnNameCache"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    private static string __GetColumnCacheKeyName(Type type, string memberName) {
        return $"<{(__GetTableCacheKeyName(type))}>.{memberName}";
    }
    #endregion

    /// <summary>
    /// Get the database table name for the specified entity type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Table class type.</typeparam>
    /// <returns>The resolved table name. Value is cached for subsequent calls.</returns>
    /// <remarks>
    /// The method uses the extension method <c>typeof(T).GetTableName()</c> to resolve the name if not already cached.
    /// Access to the underlying cache is synchronized to be safe for concurrent callers.
    /// </remarks>
    public static string GetTableName<T>() where T : class {
        return GetTableName(typeof(T));
    }

    /// <summary>
    /// Get the database table name for the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The entity <see cref="Type"/> whose table name will be resolved.</param>
    /// <returns>The resolved table name. Value is cached for subsequent calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Uses the extension method <c>type.GetTableName()</c> when a cache miss occurs.
    /// Access to the underlying cache is synchronized with an internal lock to be safe for concurrent callers.
    /// </remarks>
    public static string GetTableName(Type type) {
        string cacheKey = __GetTableCacheKeyName(type);

        lock (__TableNameCacheLock) {
            if (!__TableNameCache.ContainsKey(cacheKey)) {
                string tableName = type.GetTableName();

                __TableNameCache.Add(cacheKey, tableName);
            }

            return __TableNameCache[cacheKey];
        }
    }

    /// <summary>
    /// Get the database column name for the given member name on entity type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Table class type.</typeparam>
    /// <param name="memberName">The property or field name to resolve.</param>
    /// <returns>
    /// The resolved column name. If <paramref name="memberName"/> is null or whitespace returns an empty string.
    /// The resolved value is cached for subsequent calls.
    /// </returns>
    /// <remarks>
    /// Uses the extension method <c>typeof(T).GetColumnName(memberName)</c> when a cache miss occurs.
    /// Cache access is synchronized.
    /// </remarks>
    public static string GetColumnName<T>(string memberName) where T : class {
        return GetColumnName(typeof(T), memberName);
    }

    /// <summary>
    /// Get the database column name for the given member name on the supplied <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The entity <see cref="Type"/> whose member column will be resolved.</param>
    /// <param name="memberName">The property or field name to resolve.</param>
    /// <returns>
    /// The resolved column name. If <paramref name="memberName"/> is null or whitespace returns an empty string.
    /// The resolved value is cached for subsequent calls.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Uses the extension method <c>type.GetColumnName(memberName)</c> when a cache miss occurs.
    /// Cache access is synchronized with an internal lock.
    /// </remarks>
    public static string GetColumnName(Type type, string memberName) {
        if (type is null) throw new ArgumentNullException(nameof(type));

        if (string.IsNullOrWhiteSpace(memberName)) {
            return string.Empty;
        }

        string cacheKey = __GetColumnCacheKeyName(type, memberName);

        lock (__ColumnNameCacheLock) {
            if (!__ColumnNameCache.ContainsKey(cacheKey)) {
                string columnName = type.GetColumnName(memberName);

                __ColumnNameCache.Add(cacheKey, columnName);
            }

            return __ColumnNameCache[cacheKey];
        }
    }

    /// <summary>
    /// Get the database column name using a strongly-typed expression to identify the member.
    /// </summary>
    /// <typeparam name="T">Table class type.</typeparam>
    /// <param name="expression">Expression selecting the member (e.g. x => x.Property).</param>
    /// <returns>The resolved column name for the selected member. Returns an empty string when the member name cannot be extracted.</returns>
    /// <remarks>
    /// The expression is converted to a member name via <see cref="ExpressionHelper.ExtractClassMemberName{T}"/>.
    /// The result is then delegated to <see cref="GetColumnName{T}(string)"/>.
    /// </remarks>
    public static string GetColumnName<T>(Expression<Func<T, object>> expression) where T : class {
        string memberName = ExpressionHelper.ExtractClassMemberName<T>(expression);

        return GetColumnName<T>(memberName);
    }
}
