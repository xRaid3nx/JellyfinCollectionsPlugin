namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// How a newly created collection gets its poster.
/// </summary>
public enum PosterMode
{
    /// <summary>
    /// Look the collection up on TMDB; if there is no clear match, build a collage from its posters.
    /// </summary>
    TmdbThenCollage,

    /// <summary>
    /// Look the collection up on TMDB only.
    /// </summary>
    TmdbOnly,

    /// <summary>
    /// Leave artwork to Jellyfin.
    /// </summary>
    Off
}
