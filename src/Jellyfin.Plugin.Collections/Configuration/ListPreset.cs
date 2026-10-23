namespace Jellyfin.Plugin.Collections.Configuration;

/// <summary>
/// A saved list that can be re-run later.
/// </summary>
public class ListPreset
{
    /// <summary>
    /// Gets or sets the preset name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection name last used with this preset.
    /// </summary>
    public string LastCollectionName { get; set; } = string.Empty;
}
