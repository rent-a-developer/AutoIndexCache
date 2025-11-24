using System;
using System.Runtime.CompilerServices;
using ItemType = System.Type;

[assembly: InternalsVisibleTo("AutoIndexCache.Tests")]
[assembly: InternalsVisibleTo("AutoIndexCache.Benchmarks")]

namespace AutoIndexCache;

/// <summary>
/// Represents a thread-safe, lazy loading cache that automatically indexes cached items.
/// </summary>
public interface IAutoIndexCache
{
    /// <summary>
    /// Gets the list of cached items of the type <typeparamref name="TItem" /> from this cache.
    /// </summary>
    /// <typeparam name="TItem">The type of cache items to get.</typeparam>
    /// <returns>An instance of <see cref="IItemsList{TItem}" /> that allows to access the cached items of the type <typeparamref name="TItem" />.</returns>
    /// <exception cref="MissingItemsLoaderException">No cache items loader has been set for the cache item type <typeparamref name="TItem" /> yet.</exception>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var users = cache.Items<User>().GetAllItems();
    /// ]]>
    /// </code>
    /// </example>
    IItemsList<TItem> Items<TItem>()
        where TItem : class;

    /// <summary>
    /// Sets the delegate that loads the cache items of the type <typeparamref name="TItem" /> when cache items of that type are requested from the cache.
    /// If a cache items loader has already been set for the type <typeparamref name="TItem" /> on this instance, the old cache items loader is replaced and the corresponding <see cref="ItemsList{TItem}" /> is reset, so the new cache items loader will be used to load the cache items the next time the cache items are requested from the cache.
    /// </summary>
    /// <typeparam name="TItem">The type of cache items the loader loads.</typeparam>
    /// <param name="itemsLoader">The delegate that loads the cache items of the type <typeparamref name="TItem" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="itemsLoader" /> is null.</exception>
    /// <remarks>
    /// The specified cache items loader delegate may not return null.
    /// 
    /// The specified cache items loader delegate may not access the cached items of the type <typeparamref name="TItem" /> inside its method body.
    /// For example, the cache items loader for the cache item type "User" may not call the following methods inside its method body:
    /// <code>
    /// <![CDATA[
    /// - IItemsList<User>.GetAllItems
    /// - IItemsList<User>.Reset
    /// - IItemsList<User>.NonUniqueIndex<TKey>
    /// - IItemsList<User>.UniqueIndex<TKey>
    /// ]]>
    /// </code>
    /// However, the cache items loader is allowed to access cache items of other types.
    /// For example, the cache items loader for the cache item type "User" is allowed call the following methods inside its method body:
    /// <code>
    /// <![CDATA[
    /// - IItemsList<Group>.GetAllItems
    /// - IItemsList<Group>.Reset
    /// - IItemsList<Group>.NonUniqueIndex<TKey>
    /// - IItemsList<Group>.UniqueIndex<TKey>
    /// ]]>
    /// </code>
    /// However, cyclic dependencies are not allowed.
    /// For example, when the cache items loader of the cache item type "User" accesses cache items of the type "Group" and the cache items loader of the type "Group" accesses cache items of the type "User" (meaning User > Group > User) this is not allowed.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var cache = new AutoIndexCache();
    /// cache.SetItemsLoader<User>(() => this.LoadUsers());
    /// var users = cache.Items<User>().GetAllItems();
    /// ]]>
    /// </code>
    /// </example>
    void SetItemsLoader<TItem>(Func<TItem[]> itemsLoader)
        where TItem : class;
}

/// <summary>
/// A thread-safe, lazy loading cache that automatically indexes cached items.
/// </summary>
/// <remarks>All public and protected members of <see cref="AutoIndexCache" /> are thread-safe and may be used concurrently from multiple threads.</remarks>
#pragma warning disable CA1724
public class AutoIndexCache : IAutoIndexCache
#pragma warning restore CA1724
{
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IItemsList<TItem> Items<TItem>()
        where TItem : class
    {
        if (!this.itemsLists.TryGetValue(typeof(TItem), out var itemsList))
        {
            ThrowHelper.ThrowMissingItemsLoaderException<TItem>();
        }

        return (IItemsList<TItem>)itemsList;
    }

    /// <inheritdoc />
    public void SetItemsLoader<TItem>(Func<TItem[]> itemsLoader)
        where TItem : class
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (itemsLoader is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(itemsLoader));
        }

        this.itemsLists.AddOrUpdate(
            typeof(TItem),
            _ => new ItemsList<TItem>(itemsLoader),
            (_, itemsList) =>
            {
                ((ItemsList<TItem>)itemsList).ItemsLoader = itemsLoader;
                return itemsList;
            }
        );
    }

    private readonly CopyOnWriteDictionary<ItemType, IItemsList> itemsLists = new();
}