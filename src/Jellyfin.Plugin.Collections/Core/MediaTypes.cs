namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// Which item kinds to search.
/// </summary>
public enum MediaTypes
{
    /// <summary>
    /// Movies and series.
    /// </summary>
    Both,

    /// <summary>
    /// Movies only.
    /// </summary>
    Movies,

    /// <summary>
    /// Series only (never seasons or episodes).
    /// </summary>
    Shows
}
