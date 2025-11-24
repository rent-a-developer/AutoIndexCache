using RentADeveloper.AutoIndexCache.Exceptions;

namespace RentADeveloper.AutoIndexCache;

internal interface IUniqueIndex
{
    void Reset();
}

/// <summary>
/// Represents a unique index for cached items of the type <typeparamref name="TItem" />.
/// </summary>
/// <typeparam name="TItem">The type of cache items indexed by the index.</typeparam>
/// <typeparam name="TKey">The type of keys in the index.</typeparam>
public interface IUniqueIndex<out TItem, TKey>
    where TItem : class
{
    /// <summary>
    /// Determines whether this index contains the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if this index contains the specified key; otherwise, false.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <exception cref="DuplicateKeyException">Multiple cache items of the type <typeparamref name="TItem" /> have the same key for this unique index.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var existsUser1 = cache.Items<User>().UniqueIndex(a => a.Id).ContainsKey(1);
    /// ]]>
    /// </code>
    /// </example>
    Boolean ContainsKey(TKey? key);

    /// <summary>
    /// Gets the cached item of the type <typeparamref name="TItem" /> that satisfies the specified condition or default(<typeparamref name="TItem" />) if no such cache item was found.
    /// </summary>
    /// <param name="condition">The condition the cache item to get must satisfy.</param>
    /// <returns>The cached item of the type <typeparamref name="TItem" /> that satisfies the specified condition or default(<typeparamref name="TItem" />) if no such cache item was found.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <exception cref="DuplicateKeyException">Multiple cache items of the type <typeparamref name="TItem" /> have the same key for this unique index.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var user1 = cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
    /// ]]>
    /// </code>
    /// </example>
    TItem? GetItemOrDefault(TKey? condition);

    /// <summary>
    /// Gets the keys in this index.
    /// </summary>
    /// <returns>A read-only collection of the keys of this index.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <exception cref="DuplicateKeyException">Multiple cache items of the type <typeparamref name="TItem" /> have the same key for this unique index.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var usersIds = cache.Items<User>().UniqueIndex(a => a.Id).GetKeys();
    /// ]]>
    /// </code>
    /// </example>
    IReadOnlyCollection<TKey?> GetKeys();
}
