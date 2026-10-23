using System;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// An existing collection, for name suggestions.
/// </summary>
public sealed class CollectionSummaryDto
{
    /// <summary>
    /// Gets or sets the collection id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of items.
    /// </summary>
    public int ItemCount { get; set; }
}
