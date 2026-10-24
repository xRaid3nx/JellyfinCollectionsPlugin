namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Reads the Seerr settings from the saved plugin configuration.
/// </summary>
public sealed class PluginSeerrSettings : ISeerrSettings
{
    /// <inheritdoc />
    public string Url => Plugin.Instance?.Configuration.SeerrUrl ?? string.Empty;

    /// <inheritdoc />
    public string ApiKey => Plugin.Instance?.Configuration.SeerrApiKey ?? string.Empty;
}
