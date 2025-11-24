using RentADeveloper.AutoIndexCache.Exceptions;
using KeyExpressionString = System.String;

namespace RentADeveloper.AutoIndexCache;

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
    /// <param name="itemsLoader">The function that loads the cache items of the type <typeparamref name="TItem" />.</param>
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

                // In case a new cache items loader has been set for this list,
                // we must reset this list so the new cache items loader is used to load the cache items.
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
    public INonUniqueIndex<TItem, TKey> NonUniqueIndex<TKey>(
        Func<TItem, TKey?> keyExpression,
        [CallerArgumentExpression(nameof(keyExpression))] KeyExpressionString keyExpressionString = ""
    )
    {
        return (INonUniqueIndex<TItem, TKey>)this.nonUniqueIndexes.GetOrAdd(
            keyExpressionString,
            _ => new NonUniqueIndex<TItem, TKey>(this, keyExpression)
        );
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
    public IUniqueIndex<TItem, TKey> UniqueIndex<TKey>(
        Func<TItem, TKey?> keyExpression,
        [CallerArgumentExpression(nameof(keyExpression))] KeyExpressionString keyExpressionString = ""
    )
    {
        return (IUniqueIndex<TItem, TKey>)this.uniqueIndexes.GetOrAdd(
            keyExpressionString,
            keyExpressionString2 => new UniqueIndex<TItem, TKey>(this, keyExpression, keyExpressionString2)
        );
    }

    /// <summary>
    /// Gets a <see cref="ReadOnlySpan{T}" /> for the cached items of the type <typeparamref name="TItem" />.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}" /> for the cached items of the type <typeparamref name="TItem" />.</returns>
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader for the cache item type <typeparamref name="TItem" /> has returned a null reference instead of a list of cache items.</exception>
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
    /// <exception cref="ItemsLoaderReturnedNullException">The cache items loader returned a null reference.</exception>
    /// <exception cref="ItemsLoaderFailedException">The cache items loader threw an exception.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TItem[] LoadItems()
    {
        isCurrentThreadInsideItemsLoaderOfSameItemType = true;

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
            throw new ItemsLoaderReturnedNullException($"Could not load the cache items of the type '{typeof(TItem).FullName}'. The cache items loader for that cache item type returned a null reference. It must return a list of cache items instead.");
        }

        return loadedItems;
    }

    private readonly Object itemsLoadingLock = new();
    private readonly CopyOnWriteDictionary<KeyExpressionString, INonUniqueIndex> nonUniqueIndexes = new();
    private readonly CopyOnWriteDictionary<KeyExpressionString, IUniqueIndex> uniqueIndexes = new();
    private TItem[]? items;
    private Func<TItem[]> itemsLoader;

    /// <summary>
    /// Determines whether the current thread is inside the cache items loader for the cache item type <typeparamref name="TItem" />.
    /// </summary>
    /// <remarks>
    /// Using the <see cref="ThreadStaticAttribute" /> here works, because each type of <see cref="ItemsList{TItem}" /> gets its own <see cref="isCurrentThreadInsideItemsLoaderOfSameItemType" /> field.
    /// So for example, ItemsList{User} has a different <see cref="isCurrentThreadInsideItemsLoaderOfSameItemType" /> field than ItemsList{Group}.
    /// That way each <see cref="ItemsList{TItem}" /> can know whether the current thread is inside the cache items loader for the same cache item type.
    /// </remarks>
    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic] private static Boolean isCurrentThreadInsideItemsLoaderOfSameItemType;
}
