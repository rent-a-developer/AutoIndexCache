namespace RentADeveloper.AutoIndexCache;

/// <summary>
/// A unique index for cached items of the type <typeparamref name="TItem" />.
/// </summary>
/// <typeparam name="TItem">The type of cache items indexed by the index.</typeparam>
/// <typeparam name="TKey">The type of keys in the index.</typeparam>
/// <remarks>All public and protected members of <see cref="AutoIndexCache" /> are thread-safe and may be used concurrently from multiple threads.</remarks>
public class UniqueIndex<TItem, TKey> : IUniqueIndex, IUniqueIndex<TItem, TKey>
    where TItem : class
{
    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="itemsList">The items list the index belongs to.</param>
    /// <param name="keyExpression">The function that gets the unique index key for each cache item.</param>
    /// <param name="keyExpressionString">The string representation of <paramref name="keyExpression" />.</param>
    internal UniqueIndex(ItemsList<TItem> itemsList, Func<TItem, TKey?> keyExpression, String keyExpressionString)
    {
        this.itemsList = itemsList;
        this.keyExpression = keyExpression;
        this.keyExpressionString = keyExpressionString;
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

        return this.indexData.Value.KeyToItem.ContainsKey(key);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TItem? GetItemOrDefault(TKey? condition)
    {
        if (condition is null)
        {
            return this.indexData.Value.NullKeyItem;
        }

        this.indexData.Value.KeyToItem.TryGetValue(condition, out var item);
        return item;
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

        var keyToItem = new Dictionary<TKey, TItem>();
        TItem? nullKeyItem = null;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < itemsToIndex.Length; i++)
        {
            var item = itemsToIndex[i];
            var itemKey = this.keyExpression(item);

            if (itemKey is null)
            {
                if (nullKeyItem is not null)
                {
                    ThrowHelper.ThrowDuplicateKeyException<TItem, TKey>(itemKey, this.keyExpressionString);
                }

                nullKeyItem = item;
            }
            else
            {
                if (!keyToItem.TryAdd(itemKey, item))
                {
                    ThrowHelper.ThrowDuplicateKeyException<TItem, TKey>(itemKey, this.keyExpressionString);
                }
            }
        }

        var keys = new HashSet<TKey?>(keyToItem.Keys);

        var hasNullKey = nullKeyItem is not null;
        if (hasNullKey)
        {
            keys.Add(default);
        }

        return new(keyToItem, keys, hasNullKey, nullKeyItem);
    }

    private readonly ItemsList<TItem> itemsList;
    private readonly Func<TItem, TKey?> keyExpression;
    private readonly String keyExpressionString;
    private Lazy<IndexData> indexData;

    /// <summary>
    /// The data of an <see cref="UniqueIndex{TItem,TKey}" />.
    /// </summary>
    /// <param name="keyToItem">Maps a key to the cache item that has that key.</param>
    /// <param name="keys">The unique keys in the index.</param>
    /// <param name="containsNullKey">Determines whether the index contains a null key.</param>
    /// <param name="nullKeyItem">The cache item that has null as its key or null in case no such cache item exists.</param>
    private sealed class IndexData(Dictionary<TKey, TItem> keyToItem, HashSet<TKey?> keys, Boolean containsNullKey, TItem? nullKeyItem)
    {
        public readonly Boolean ContainsNullKey = containsNullKey;
        public readonly HashSet<TKey?> Keys = keys;
        public readonly Dictionary<TKey, TItem> KeyToItem = keyToItem;
        public readonly TItem? NullKeyItem = nullKeyItem;
    }
}
