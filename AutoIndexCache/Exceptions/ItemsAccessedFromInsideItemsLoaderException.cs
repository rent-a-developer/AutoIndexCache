namespace AutoIndexCache.Exceptions;

/// <summary>
/// Thrown when an attempt was made to access cached items of certain type from inside the cache items loader for that cache item type (e.g. an attempt was made to access cached Users from inside the cache items loader for the User type).
/// </summary>
#pragma warning disable CA1032
public class ItemsAccessedFromInsideItemsLoaderException : Exception
#pragma warning restore CA1032
{
    /// <inheritdoc />
    public ItemsAccessedFromInsideItemsLoaderException(String message) : base(message)
    {
    }
}
