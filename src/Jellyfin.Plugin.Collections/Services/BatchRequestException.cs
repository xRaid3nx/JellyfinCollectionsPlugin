using System;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// A request problem the user can fix. The message is safe to show and becomes an HTTP 400.
/// </summary>
public sealed class BatchRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRequestException"/> class.
    /// </summary>
    public BatchRequestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRequestException"/> class.
    /// </summary>
    /// <param name="message">A readable message.</param>
    public BatchRequestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRequestException"/> class.
    /// </summary>
    /// <param name="message">A readable message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BatchRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
