using System;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// An item id that could not be added.
/// </summary>
public sealed class FailedItemDto
{
    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
