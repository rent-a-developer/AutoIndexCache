using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using KeyExpressionString = System.String;

namespace AutoIndexCache;

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
    /// Per default cache items are lazily loaded when they are requested from the cache (e.g. when <see cref="ItemsList{TItem}.GetAllItems" /> is called).
    /// This method loads the cache items immediately.
    /// </remarks>
    void ForceLoadItems();

    /// <summary>
    /// Gets all cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <returns>A read-only list of all cached items of the type <typeparamref name="TItem" />.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null value instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var users = cache.Items<User>().GetAllItems();
    /// ]]>
    /// </code>
    /// </example>
    IReadOnlyList<TItem> GetAllItems();

    /// <summary>
    /// Gets a non-unique index for the cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the index.</typeparam>
    /// <param name="keyExpression">A delegate that gets the index key for each cached item.</param>
    /// <param name="keyExpressionString">The string representation of <paramref name="keyExpression" />.</param>
    /// <returns>An instance of <see cref="INonUniqueIndex{TItem,TKey}" /> that provides access to the specified non-unique index.</returns>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var usersOfGroup1 = cache.Items<User>().NonUniqueIndex(a => a.GroupId).GetAll(1);
    /// var activeUsersOfGroup10 = cache.Items<User>().NonUniqueIndex(a => new { a.IsActive, a.GroupId}).GetItems(new { IsActive = true, GroupId = 10 });
    /// ]]>
    /// </code>
    /// </example>
    INonUniqueIndex<TItem, TKey> NonUniqueIndex<TKey>(Func<TItem, TKey?> keyExpression, [CallerArgumentExpression(nameof(keyExpression))] KeyExpressionString keyExpressionString = "");

    /// <summary>
    /// Removes all cache items of the type <typeparamref name="TItem" /> from the cache.
    /// The next time cache items of the type <typeparamref name="TItem" /> are requested from the cache, they are loaded again using the cache items loader for that cache item type (<see cref="AutoIndexCache.SetItemsLoader{TItem}" />).
    /// </summary>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to reset the cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
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
    /// <param name="keyExpression">A delegate that gets the index key for each cached item.</param>
    /// <param name="keyExpressionString">The string representation <paramref name="keyExpression" />.</param>
    /// <returns>An instance of <see cref="IUniqueIndex{TItem,TKey}" /> that provides access to the specified unique index.</returns>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var user1 = cache.Items<User>().UniqueIndex(a => a.Id).GetItemOrDefault(1);
    /// ]]>
    /// </code>
    /// </example>
    IUniqueIndex<TItem, TKey> UniqueIndex<TKey>(Func<TItem, TKey?> keyExpression, [CallerArgumentExpression(nameof(keyExpression))] KeyExpressionString keyExpressionString = "");

    /// <summary>
    /// The cache items loader that loads the cache items for this instance.
    /// </summary>
    internal Func<TItem[]> ItemsLoader { get; set; }
}

/// <summary>
/// A list of cached items of the type <typeparamref name="TItem" />.
/// Allows to access the cache items of that type, to access unique and non-unique indexes for that type and to reset the list.
/// </summary>
/// <typeparam name="TItem">The type of cached items the list contains.</typeparam>
/// <remarks>All public and protected members of <see cref="ItemsList{TItem}" /> are thread-safe and may be used concurrently from multiple threads.</remarks>
public class ItemsList<TItem> : IItemsList, IItemsList<TItem>
    where TItem : class
{
    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="itemsLoader">The delegate that loads the cache items of the type <typeparamref name="TItem" />.</param>
    internal ItemsList(Func<TItem[]> itemsLoader)
    {
        this.itemsLoader = itemsLoader;
    }

    /// <inheritdoc />
    public Func<TItem[]> ItemsLoader
    {
        get => this.itemsLoader;
        set
        {
            if (value != this.itemsLoader)
            {
                this.itemsLoader = value;

                // In case a new cache items loader has been set for this list, we must reset this list so the new cache items loader is used to get the cache items.
                this.Reset();
            }
        }
    }

    /// <inheritdoc />
    public void ForceLoadItems()
    {
        this.GetOrLoadItems();
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<TItem> GetAllItems()
    {
        return this.GetOrLoadItems();
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public INonUniqueIndex<TItem, TKey> NonUniqueIndex<TKey>(Func<TItem, TKey?> keyExpression, [CallerArgumentExpression(nameof(keyExpression))] KeyExpressionString keyExpressionString = "")
    {
        return (INonUniqueIndex<TItem, TKey>)this.nonUniqueIndexes.GetOrAdd(keyExpressionString, _ => new NonUniqueIndex<TItem, TKey>(this, keyExpression));
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (isCurrentThreadInsideItemsLoaderOfSameItemType)
        {
            throw new ItemsAccessedFromInsideItemsLoaderException($"Cannot reset the cache items of the type '{typeof(TItem).FullName}'. The current thread is inside the cache items loader for that cache item type.");
        }

        Volatile.Write(ref this.items, null);

        foreach (var index in this.nonUniqueIndexes.Values)
        {
            index.Reset();
        }

        foreach (var index in this.uniqueIndexes.Values)
        {
            index.Reset();
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUniqueIndex<TItem, TKey> UniqueIndex<TKey>(Func<TItem, TKey?> keyExpression, [CallerArgumentExpression(nameof(keyExpression))] KeyExpressionString keyExpressionString = "")
    {
        // ReSharper disable once VariableHidesOuterVariable
        return (IUniqueIndex<TItem, TKey>)this.uniqueIndexes.GetOrAdd(keyExpressionString, keyExpressionString => new UniqueIndex<TItem, TKey>(this, keyExpression, keyExpressionString));
    }

    /// <summary>
    /// Gets a <see cref="ReadOnlySpan{T}" /> over the cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}" /> over the cached items of the type <typeparamref name="TItem" />.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null value instead of a list of cache items.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader for the cache item type <typeparamref name="TItem" /> has thrown an exception.</exception>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var users = cache.Items<User>().GetAllItems();
    /// ]]>
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<TItem> GetAllItemsSpan()
    {
        return this.GetOrLoadItems();
    }

    /// <summary>
    /// Gets the cache items of this instance and loads them if they have not been loaded yet.
    /// </summary>
    /// <returns>The cache items of this instance.</returns>
    /// <exception cref="ItemsAccessedFromInsideItemsLoaderException">An attempt was made to access cache items of the type <typeparamref name="TItem" /> from inside the cache items loader for that cache item type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TItem[] GetOrLoadItems()
    {
        if (isCurrentThreadInsideItemsLoaderOfSameItemType)
        {
            ThrowHelper.ThrowItemsAccessedFromInsideItemsLoaderException<TItem>();
        }

        // Fast path - items are already loaded.
        var currentItems = Volatile.Read(ref this.items);
        if (currentItems is not null)
        {
            return currentItems;
        }

        // Slow path - items need to be loaded.
        // We need to lock this section, so the items loader is only ever executed by one thread.
        lock (this.itemsLoadingLock)
        {
            // We got the lock, but we need to double check if the items are still not loaded.
            currentItems = Volatile.Read(ref this.items);
            if (currentItems is not null)
            {
                return currentItems;
            }

            var newItems = this.LoadItems();
            Volatile.Write(ref this.items, newItems);
            return newItems;
        }
    }

    /// <summary>
    /// Loads the cache items for this instance.
    /// </summary>
    /// <returns>The loaded cache items.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader returned a null value.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader threw an exception.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TItem[] LoadItems()
    {
#pragma warning disable S2696
        isCurrentThreadInsideItemsLoaderOfSameItemType = true;
#pragma warning restore S2696

        TItem[]? loadedItems;

        try
        {
            loadedItems = this.ItemsLoader();
        }
        catch (Exception ex)
        {
            throw new ItemsLoaderFailedException($"Could not load the cache items of the type '{typeof(TItem).FullName}'. The cache items loader for that cache item type threw an exception. See the inner exception for details.", ex);
        }
        finally
        {
            isCurrentThreadInsideItemsLoaderOfSameItemType = false;
        }

        if (loadedItems is null)
        {
            throw new ItemsLoaderReturnedNullException($"Could not load the cache items of the type '{typeof(TItem).FullName}'. The cache items loader for that cache item type returned a null value. It must return a list of cache items instead.");
        }

        return loadedItems;
    }

    private readonly Object itemsLoadingLock = new();

    private readonly CopyOnWriteDictionary<KeyExpressionString, INonUniqueIndex> nonUniqueIndexes = new();
    private readonly CopyOnWriteDictionary<KeyExpressionString, IUniqueIndex> uniqueIndexes = new();
    private TItem[]? items;
    private Func<TItem[]> itemsLoader;

    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Determines whether the current thread is inside the cache items loader for the cache item type <typeparamref name="TItem" />.
    /// </summary>
    /// <remarks>
    /// Using the <see cref="ThreadStaticAttribute" /> here works, because each type of ItemsList gets its own <see cref="isCurrentThreadInsideItemsLoaderOfSameItemType" /> field.
    /// So for example, ItemsList{User} has a different <see cref="isCurrentThreadInsideItemsLoaderOfSameItemType" /> field than ItemsList{Group}.
    /// That way each ItemsList can know whether the current thread is inside the cache items loader for the same cache item type.
    /// </remarks>
    [ThreadStatic] private static Boolean isCurrentThreadInsideItemsLoaderOfSameItemType;
}