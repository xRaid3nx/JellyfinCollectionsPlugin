using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Response for <c>POST /PasteCollections/Apply</c>.
/// </summary>
public sealed class ApplyResponse
{
    /// <summary>
    /// Gets or sets the collection id.
    /// </summary>
    public Guid CollectionId { get; set; }

    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the collection was created by this call.
    /// </summary>
    public bool Created { get; set; }

    /// <summary>
    /// Gets or sets the number of items added.
    /// </summary>
    public int Added { get; set; }

    /// <summary>
    /// Gets or sets the number of confirmed items that were already in the collection.
    /// </summary>
    public int AlreadyPresent { get; set; }

    /// <summary>
    /// Gets the items that were rejected, with reasons.
    /// </summary>
    public IReadOnlyList<FailedItemDto> Failed { get; init; } = [];

    /// <summary>
    /// Gets or sets a warning, if any.
    /// </summary>
    public string? Warning { get; set; }
}
