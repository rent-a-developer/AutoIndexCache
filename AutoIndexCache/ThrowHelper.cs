using RentADeveloper.AutoIndexCache.Exceptions;

namespace RentADeveloper.AutoIndexCache;

/// <summary>
/// Provides helper methods to throw exceptions.
/// </summary>
/// <remarks>
/// The main purpose of this class is to reduce code size and increase performance.
///
/// Extracting throw logic to separate methods is a good idea in performance-critical paths because:
/// - It encourages method inlining (smaller methods are more likely to be inlined).
/// - It avoids polluting optimized code paths with exception logic.
/// - It makes code more maintainable by separating normal logic from error handling.
/// </remarks>
internal static class ThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentNullException(String parameterName)
    {
        throw new ArgumentNullException(parameterName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDuplicateKeyException<TItem, TKey>(TKey? itemKey, String keyExpressionString)
    {
        throw new DuplicateKeyException($"Duplicate key found: Multiple cache items of the type '{typeof(TItem).FullName}' have the same key '{(itemKey is null ? "{null}" : itemKey)}' for the key expression '{keyExpressionString}'.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowItemsAccessedFromInsideItemsLoaderException<TItem>()
    {
        throw new ItemsAccessedFromInsideItemsLoaderException($"Cannot access the cache items of the type '{typeof(TItem).FullName}'. The current thread is inside the cache items loader for that cache item type.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMissingItemsLoaderException<TItem>() where TItem : class
    {
        throw new MissingItemsLoaderException($"Cannot get cache items of type '{typeof(TItem).FullName}'. No cache items loader for this cache item type is set on this instance. Use the method {nameof(AutoIndexCache)}.{nameof(AutoIndexCache.SetItemsLoader)} to set a cache items loader for the cache item type before trying to access the cache items of that type.");
    }
}
