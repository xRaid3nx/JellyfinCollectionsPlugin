using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// One title to request in Seerr.
/// </summary>
public sealed class SeerrRequestItem
{
    /// <summary>
    /// Gets or sets the list line number, echoed back in the result.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the parsed title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the parsed year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets which kinds to consider when searching.
    /// </summary>
    public MediaTypes MediaTypes { get; set; }

    /// <summary>
    /// Gets or sets a TMDB id the user picked. Skips the search when set together with <see cref="MediaType"/>.
    /// </summary>
    public int? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the Seerr media type for <see cref="TmdbId"/>: <c>movie</c> or <c>tv</c>.
    /// </summary>
    public string? MediaType { get; set; }
}
