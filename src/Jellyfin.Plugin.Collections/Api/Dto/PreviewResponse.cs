using System.Collections.Generic;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Response for <c>POST /BatchCollections/Preview</c>.
/// </summary>
public sealed class PreviewResponse
{
    /// <summary>
    /// Gets or sets the target collection.
    /// </summary>
    public TargetCollectionDto Collection { get; set; } = new();

    /// <summary>
    /// Gets the per-line results.
    /// </summary>
    public IReadOnlyList<PreviewLineDto> Lines { get; init; } = [];

    /// <summary>
    /// Gets the lines skipped as duplicates.
    /// </summary>
    public IReadOnlyList<DuplicateLineDto> DuplicateLines { get; init; } = [];

    /// <summary>
    /// Gets the count per status.
    /// </summary>
    public IReadOnlyDictionary<MatchStatus, int> Counts { get; init; } = new Dictionary<MatchStatus, int>();

    /// <summary>
    /// Gets or sets the number of items searched.
    /// </summary>
    public int LibraryItemCount { get; set; }

    /// <summary>
    /// Gets or sets the number of blank and comment lines ignored.
    /// </summary>
    public int IgnoredLineCount { get; set; }
}
