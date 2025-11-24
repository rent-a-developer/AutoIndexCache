using System;

namespace AutoIndexCache;

/// <summary>
/// Thrown when a cache items loader has returned a null value instead of a list of cache items.
/// </summary>
#pragma warning disable CA1032
public class ItemsLoaderReturnedNullException : Exception
#pragma warning restore CA1032
{
    /// <inheritdoc />
    public ItemsLoaderReturnedNullException(String message) : base(message)
    {
    }
}