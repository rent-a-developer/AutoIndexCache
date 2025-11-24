namespace RentADeveloper.AutoIndexCache;

/// <summary>
/// A non-unique index for cached items of the type <typeparamref name="TItem" />.
/// </summary>
/// <typeparam name="TItem">The type of cache items indexed by the index.</typeparam>
/// <typeparam name="TKey">The type of keys in the index.</typeparam>
/// <remarks>All public and protected members of <see cref="NonUniqueIndex{TItem,TKey}" /> are thread-safe and may be used concurrently from multiple threads.</remarks>
public class NonUniqueIndex<TItem, TKey> : INonUniqueIndex, INonUniqueIndex<TItem, TKey>
    where TItem : class
{
    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="itemsList">The items list the index belongs to.</param>
    /// <param name="keyExpression">The function that gets the non-unique index key for each cache item.</param>
    internal NonUniqueIndex(ItemsList<TItem> itemsList, Func<TItem, TKey?> keyExpression)
    {
        this.itemsList = itemsList;
        this.keyExpression = keyExpression;
        this.indexData = new(this.CreateIndexData);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Boolean ContainsKey(TKey? key)
    {
        if (key is null)
        {
            return this.indexData.Value.ContainsNullKey;
        }

        return this.indexData.Value.KeyToItems.ContainsKey(key);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<TItem> GetItems(TKey? condition)
    {
        if (condition is null)
        {
            return this.indexData.Value.NullKeyItems;
        }

        if (this.indexData.Value.KeyToItems.TryGetValue(condition, out var matchingItems))
        {
            return matchingItems;
        }

        return ArraySegment<TItem>.Empty;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyCollection<TKey?> GetKeys()
    {
        return this.indexData.Value.Keys;
    }

    /// <inheritdoc />
    public void Reset()
    {
        this.indexData = new(this.CreateIndexData);
    }

    /// <summary>
    /// Creates up-to-date data for this index.
    /// </summary>
    /// <returns>The up-to-date data for this index.</returns>
    private IndexData CreateIndexData()
    {
        var itemsToIndex = this.itemsList.GetAllItemsSpan();

        var keyToItems = new Dictionary<TKey, List<TItem>>();
        var nullKeyItems = new List<TItem>();

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < itemsToIndex.Length; i++)
        {
            var item = itemsToIndex[i];
            var itemKey = this.keyExpression(item);

            if (itemKey is null)
            {
                nullKeyItems.Add(item);
            }
            else
            {
                if (!keyToItems.TryGetValue(itemKey, out var itemsHavingKey))
                {
                    itemsHavingKey = [];
                    keyToItems.Add(itemKey, itemsHavingKey);
                }

                itemsHavingKey.Add(item);
            }
        }

        var keys = new HashSet<TKey?>(keyToItems.Keys);

        var hasNullKey = nullKeyItems.Count != 0;
        if (hasNullKey)
        {
            keys.Add(default);
        }

        return new(keyToItems, keys, hasNullKey, nullKeyItems);
    }

    private readonly ItemsList<TItem> itemsList;
    private readonly Func<TItem, TKey?> keyExpression;
    private Lazy<IndexData> indexData;

    /// <summary>
    /// The data of a <see cref="NonUniqueIndex{TItem,TKey}" />.
    /// </summary>
    /// <param name="keyToItems">Maps a key to the cache items that have that key.</param>
    /// <param name="keys">The unique keys in the index.</param>
    /// <param name="containsNullKey">Determines whether the index contains a null key.</param>
    /// <param name="nullKeyItems">A list of cache items that have null as their key.</param>
    private sealed class IndexData(Dictionary<TKey, List<TItem>> keyToItems, HashSet<TKey?> keys, Boolean containsNullKey, IReadOnlyList<TItem> nullKeyItems)
    {
        public readonly Boolean ContainsNullKey = containsNullKey;
        public readonly HashSet<TKey?> Keys = keys;
        public readonly Dictionary<TKey, List<TItem>> KeyToItems = keyToItems;
        public readonly IReadOnlyList<TItem> NullKeyItems = nullKeyItems;
    }
}
