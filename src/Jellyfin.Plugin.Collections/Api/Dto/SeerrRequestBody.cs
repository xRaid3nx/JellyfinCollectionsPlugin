using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Request body for <c>POST /BatchCollections/Seerr/Request</c>.
/// </summary>
public sealed class SeerrRequestBody
{
    /// <summary>
    /// Gets or sets the titles to request.
    /// </summary>
    public IReadOnlyList<SeerrRequestItem>? Items { get; set; }
}
