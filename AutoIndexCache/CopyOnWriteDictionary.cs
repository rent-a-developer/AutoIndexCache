namespace AutoIndexCache;

/// <summary>
/// A thread-safe, append-only dictionary with internal copy-on-write behavior.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
internal class CopyOnWriteDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets a snapshot of all values in this instance.
    /// </summary>
    /// <remarks>
    /// This property returns a copy of all the values in this instance.
    /// The returned sequence is not kept in sync with this instance.
    /// </remarks>
    public IEnumerable<TValue> Values => Volatile.Read(ref this.dictionary).Values;

    /// <summary>
    /// Adds a key/value pair to this instance if the key does not already
    /// exist, or updates a key/value pair in this instance if the key
    /// already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="addValueFactory">The function used to generate a value for an absent key.</param>
    /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is a null reference.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="addValueFactory" /> is a null reference.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updateValueFactory" /> is a null reference.</exception>
    /// <exception cref="OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>
    /// The new value for the key.
    /// This will be either be the result of <paramref name="addValueFactory" /> (if the key was absent) or the result of <paramref name="updateValueFactory" /> (if the key was present).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (key is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(key));
        }

        if (addValueFactory is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(addValueFactory));
        }

        if (updateValueFactory is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(updateValueFactory));
        }

        lock (this.lockObject)
        {
            var snapshot = Volatile.Read(ref this.dictionary);
            Dictionary<TKey, TValue> newDictionary;

            if (snapshot.TryGetValue(key, out var existingValue))
            {
                var newValue = updateValueFactory(key, existingValue);

                newDictionary = new(snapshot);
                newDictionary[key] = newValue;
                Volatile.Write(ref this.dictionary, newDictionary);

                return newValue;
            }

            var value = addValueFactory(key);
            newDictionary = new(snapshot);
            newDictionary.Add(key, value);
            Volatile.Write(ref this.dictionary, newDictionary);

            return value;
        }
    }

    /// <summary>
    /// Adds a key/value pair to this instance if the key does not already exist.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is a null reference.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory" /> is a null reference.</exception>
    /// <exception cref="OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>
    /// The value for the key.
    /// This will be either the existing value for the key if the key is already in this instance,
    /// or the new value for the key as returned by <paramref name="valueFactory" /> if the key was not in this instance.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (key is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(key));
        }

        if (valueFactory is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(valueFactory));
        }

        var snapshot = Volatile.Read(ref this.dictionary);

        if (snapshot.TryGetValue(key, out var value))
        {
            return value;
        }

        lock (this.lockObject)
        {
            snapshot = Volatile.Read(ref this.dictionary);

            if (snapshot.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key);

            var newDictionary = new Dictionary<TKey, TValue>(snapshot);
            newDictionary.Add(key, value);

            Volatile.Write(ref this.dictionary, newDictionary);

            return value;
        }
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key from the this instance.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">
    /// When this method returns, <paramref name="value" /> contains the object from
    /// this instance with the specified key or the default value of
    /// <typeparamref name="TValue" />, if the operation failed.
    /// </param>
    /// <returns>true if the key was found in this instance; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is a null reference.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Boolean TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (key is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(key));
        }

        var snapshot = Volatile.Read(ref this.dictionary);

        return snapshot.TryGetValue(key, out value);
    }

    private readonly Object lockObject = new();
    private Dictionary<TKey, TValue> dictionary = new();
}
