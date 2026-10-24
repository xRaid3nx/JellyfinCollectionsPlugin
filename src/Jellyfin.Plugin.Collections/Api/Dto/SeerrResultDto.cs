using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// The Seerr outcome for one title.
/// </summary>
public sealed class SeerrResultDto
{
    /// <summary>
    /// Gets or sets the list line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the outcome.
    /// </summary>
    public SeerrOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets a readable explanation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Seerr item that was requested or checked.
    /// </summary>
    public SeerrCandidateDto? Item { get; set; }

    /// <summary>
    /// Gets the choices when <see cref="Outcome"/> is <see cref="SeerrOutcome.NeedsChoice"/>.
    /// </summary>
    public IReadOnlyList<SeerrCandidateDto> Candidates { get; init; } = [];
}
