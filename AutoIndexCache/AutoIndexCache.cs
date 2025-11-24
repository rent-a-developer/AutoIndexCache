[assembly: InternalsVisibleTo("AutoIndexCache.Tests")]
[assembly: InternalsVisibleTo("AutoIndexCache.Benchmarks")]

namespace AutoIndexCache;

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

    private readonly CopyOnWriteDictionary<Type, IItemsList> itemsLists = new();
}
