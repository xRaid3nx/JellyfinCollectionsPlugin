namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// A list line skipped because it repeats an earlier line.
/// </summary>
public sealed class DuplicateLineDto
{
    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the raw text.
    /// </summary>
    public string Raw { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line it repeats.
    /// </summary>
    public int DuplicateOfLine { get; set; }
}
