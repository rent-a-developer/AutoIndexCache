using RentADeveloper.AutoIndexCache.Exceptions;

namespace RentADeveloper.AutoIndexCache;

internal interface INonUniqueIndex
{
    void Reset();
}

/// <summary>
/// Represents a non-unique index for cached items of the type <typeparamref name="TItem" />.
/// </summary>
/// <typeparam name="TItem">The type of cache items indexed by the index.</typeparam>
/// <typeparam name="TKey">The type of keys in the index.</typeparam>
public interface INonUniqueIndex<out TItem, TKey>
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
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var hasGroup1Users = cache.Items<User>().NonUniqueIndex(a => a.GroupId).ContainsKey(1);
    /// ]]>
    /// </code>
    /// </example>
    Boolean ContainsKey(TKey? key);

    /// <summary>
    /// Gets all cached items of the type <typeparamref name="TItem" /> that have the specified key.
    /// </summary>
    /// <param name="key">The key of the cache items to get.</param>
    /// <returns>A read-only list of cached items of the type <typeparamref name="TItem" /> that have the specified key.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var usersOfGroup1 = cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetItems(1);
    /// var activeUsersOfGroup10 = cache.Items<User>().NonUniqueIndex(a => new { a.IsActive, a.GroupId}).GetItems(new { IsActive = true, GroupId = 10 });
    /// ]]>
    /// </code>
    /// </example>
    IReadOnlyList<TItem> GetItems(TKey? key);

    /// <summary>
    /// Gets the keys in this index.
    /// </summary>
    /// <returns>A read-only collection of the keys in this index.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var distinctGroupIds = cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetKeys();
    /// ]]>
    /// </code>
    /// </example>
    IReadOnlyCollection<TKey?> GetKeys();
}
