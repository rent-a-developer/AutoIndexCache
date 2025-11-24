using System;

namespace AutoIndexCache;

/// <summary>
/// Thrown when a cache items loader has thrown an exception.
/// </summary>
#pragma warning disable CA1032
public class ItemsLoaderFailedException : Exception
#pragma warning restore CA1032
{
    /// <inheritdoc />
    public ItemsLoaderFailedException(String message, Exception innerException) : base(message, innerException)
    {
    }
}