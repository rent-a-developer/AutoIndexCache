using System;

namespace AutoIndexCache;

/// <summary>
/// Thrown when multiple cache items have the same key for a unique index.
/// </summary>
#pragma warning disable CA1032
public class DuplicateKeyException : Exception
#pragma warning restore CA1032
{
    /// <inheritdoc />
    public DuplicateKeyException(String message) : base(message)
    {
    }
}