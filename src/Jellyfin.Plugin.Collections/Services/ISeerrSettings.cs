namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Where Seerr is and how to authenticate.
/// </summary>
public interface ISeerrSettings
{
    /// <summary>
    /// Gets the Seerr base URL.
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Gets the Seerr API key.
    /// </summary>
    string ApiKey { get; }
}
