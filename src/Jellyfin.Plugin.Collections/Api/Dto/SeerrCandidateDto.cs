namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// A Seerr (TMDB) search result.
/// </summary>
public sealed class SeerrCandidateDto
{
    /// <summary>
    /// Gets or sets the TMDB id.
    /// </summary>
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the Seerr media type: <c>movie</c> or <c>tv</c>.
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the release or first-air year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the Seerr media status (1 unknown, 2 pending, 3 processing, 4 partly available, 5 available, 6 deleted), or 0 if Seerr has no record.
    /// </summary>
    public int Status { get; set; }
}
