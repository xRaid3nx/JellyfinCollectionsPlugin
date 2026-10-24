namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// Request body for <c>POST /BatchCollections/Seerr/Test</c>. Blank values fall back to the saved settings.
/// </summary>
public sealed class SeerrTestRequest
{
    /// <summary>
    /// Gets or sets the Seerr base URL to test.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the API key to test.
    /// </summary>
    public string? ApiKey { get; set; }
}
