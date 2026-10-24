using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.Collections.Core;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Collections.Configuration;

/// <summary>
/// Saved options and presets. Never stores item ids or library data.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the last-used media type.
    /// </summary>
    public MediaTypes LastMediaTypes { get; set; } = MediaTypes.Both;

    /// <summary>
    /// Gets or sets the last-used year tolerance (0 or 1).
    /// </summary>
    public int LastYearTolerance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fuzzy matching was last enabled.
    /// </summary>
    public bool LastFuzzy { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether "lock metadata" was last enabled.
    /// </summary>
    public bool LastLockMetadata { get; set; }

    /// <summary>
    /// Gets or sets the last-used poster option for new collections.
    /// </summary>
    public PosterMode LastPosterMode { get; set; } = PosterMode.TmdbThenCollage;

    /// <summary>
    /// Gets or sets the last collection name.
    /// </summary>
    public string LastCollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Seerr base URL, as reachable from the Jellyfin server.
    /// </summary>
    public string SeerrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Seerr API key (Seerr: Settings, General).
    /// </summary>
    public string SeerrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the saved list presets.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Required for XML serialization.")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Required for XML serialization.")]
    public List<ListPreset> Presets { get; set; } = [];
}
