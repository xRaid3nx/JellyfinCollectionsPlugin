using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Request body for <c>POST /BatchCollections/Apply</c>.
/// </summary>
public sealed class ApplyRequest
{
    /// <summary>
    /// Gets or sets the target collection name.
    /// </summary>
    public string? CollectionName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to lock metadata on a newly created collection.
    /// </summary>
    public bool LockMetadata { get; set; }

    /// <summary>
    /// Gets or sets the confirmed item ids.
    /// </summary>
    public IReadOnlyList<Guid>? ItemIds { get; set; }
}
