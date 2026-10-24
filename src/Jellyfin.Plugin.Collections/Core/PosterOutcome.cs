namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// Which poster a newly created collection ended up with.
/// </summary>
public enum PosterOutcome
{
    /// <summary>
    /// Nothing was attempted.
    /// </summary>
    NotAttempted,

    /// <summary>
    /// The collection was identified on TMDB.
    /// </summary>
    Tmdb,

    /// <summary>
    /// A collage was built from the members' posters.
    /// </summary>
    Collage,

    /// <summary>
    /// The only member with a poster lent its poster.
    /// </summary>
    SinglePoster,

    /// <summary>
    /// No poster could be found or made.
    /// </summary>
    None
}
