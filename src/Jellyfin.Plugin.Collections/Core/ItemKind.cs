namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// The kinds of library item the plugin can add to a collection.
/// </summary>
public enum ItemKind
{
    /// <summary>
    /// A movie.
    /// </summary>
    Movie,

    /// <summary>
    /// A TV series (never a season or episode).
    /// </summary>
    Series
}
