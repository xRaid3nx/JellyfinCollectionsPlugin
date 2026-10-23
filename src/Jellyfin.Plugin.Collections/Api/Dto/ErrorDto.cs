namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// A readable error message.
/// </summary>
public sealed class ErrorDto
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
