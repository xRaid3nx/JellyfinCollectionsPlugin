using System;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Information about the target collection.
/// </summary>
public sealed class TargetCollectionDto
{
    /// <summary>
    /// Gets or sets the name as typed (trimmed).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a collection with this name exists.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// Gets or sets the existing collection id.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the number of items already in the collection.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets a warning (for example, several collections share the name).
    /// </summary>
    public string? Warning { get; set; }
}
