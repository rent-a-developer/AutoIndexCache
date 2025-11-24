using RentADeveloper.AutoIndexCache.Exceptions;

namespace RentADeveloper.AutoIndexCache;

internal interface IItemsList;

/// <summary>
/// Represents a list of cached items of the type <typeparamref name="TItem" />.
/// Allows to access the cache items of that type, to access unique and non-unique indexes for that type and to reset the list.
/// </summary>
/// <typeparam name="TItem">The type of cached items the list contains.</typeparam>
public interface IItemsList<TItem> where TItem : class
{
    /// <summary>
    /// Forces this list to load the cache items of the type <typeparamref name="TItem" /> immediately, using the cache items loader specified for the cache item type <typeparamref name="TItem" />.
    /// </summary>
    /// <remarks>
    /// Normally cache items are lazily loaded when they are requested from the cache (e.g. when <see cref="IItemsList{TItem}.GetAllItems" /> is called).
    /// This method loads the cache items immediately.
    /// </remarks>
    void ForceLoadItems();

    /// <summary>
    /// Gets all cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <returns>A read-only list of all cached items of the type <typeparamref name="TItem" />.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var users = cache.Items<User>().GetAllItems();
    /// ]]>
    /// </code>
    /// </example>
    IReadOnlyList<TItem> GetAllItems();

    /// <summary>
    /// Gets a non-unique index for the cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the index.</typeparam>
    /// <param name="keyExpression">A function that gets the index key for each cached item.</param>
    /// <param name="keyExpressionString">The string representation of <paramref name="keyExpression" />.</param>
    /// <returns>An instance of <see cref="INonUniqueIndex{TItem,TKey}" /> that provides access to the specified non-unique index.</returns>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var usersOfGroup1 = cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetAll(1);
    /// var activeUsersOfGroup10 = cache.Items<User>().NonUniqueIndex(a => new { a.IsActive, a.GroupId}).GetItems(new { IsActive = true, GroupId = 10 });
    /// ]]>
    /// </code>
    /// </example>
    INonUniqueIndex<TItem, TKey> NonUniqueIndex<TKey>(
        Func<TItem, TKey?> keyExpression,
        [CallerArgumentExpression(nameof(keyExpression))] String keyExpressionString = ""
    );

    /// <summary>
    /// Removes all cache items of the type <typeparamref name="TItem" /> from the cache.
    /// The next time cache items of the type <typeparamref name="TItem" /> are requested from the cache, they are loaded again using the cache items loader for that cache item type (<see cref="AutoIndexCache.SetItemsLoader{TItem}" />).
    /// </summary>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to reset the cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var users = cache.Items<User>().GetAllItems();
    /// cache.Items<User>().Reset();
    /// var updatedUsers = cache.Items<User>().GetAllItems(); // this.LoadUsers will be called again to get the updated list of users.
    /// ]]>
    /// </code>
    /// </example>
    void Reset();

    /// <summary>
    /// Gets a unique index for the cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the index.</typeparam>
    /// <param name="keyExpression">A function that gets the index key for each cached item.</param>
    /// <param name="keyExpressionString">The string representation <paramref name="keyExpression" />.</param>
    /// <returns>An instance of <see cref="IUniqueIndex{TItem,TKey}" /> that provides access to the specified unique index.</returns>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(this.LoadUsers);
    /// var user1 = cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
    /// ]]>
    /// </code>
    /// </example>
    IUniqueIndex<TItem, TKey> UniqueIndex<TKey>(
        Func<TItem, TKey?> keyExpression,
        [CallerArgumentExpression(nameof(keyExpression))] String keyExpressionString = ""
    );

    /// <summary>
    /// The cache items loader that loads the cache items for this instance.
    /// </summary>
    internal Func<TItem[]> ItemsLoader { get; set; }
}
