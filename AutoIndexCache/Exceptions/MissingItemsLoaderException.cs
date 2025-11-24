namespace AutoIndexCache.Exceptions;

/// <summary>
/// Thrown when an attempt was made to access cached items of certain type when no cache items loader has been set yet for that cache item type.
/// </summary>
#pragma warning disable CA1032
public class MissingItemsLoaderException : Exception
#pragma warning restore CA1032
{
    /// <inheritdoc />
    public MissingItemsLoaderException(String message) : base(message)
    {
    }
}
