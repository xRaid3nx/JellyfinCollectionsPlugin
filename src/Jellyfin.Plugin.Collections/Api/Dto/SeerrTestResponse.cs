namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Result of a Seerr connection test.
/// </summary>
public sealed class SeerrTestResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether Seerr answered and accepted the API key.
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    /// Gets or sets a readable explanation.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
