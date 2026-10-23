using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// The matching outcome for one list line.
/// </summary>
/// <param name="Line">The parsed line.</param>
/// <param name="Status">The match status.</param>
/// <param name="SelectedId">The suggested item (pre-selected only when <paramref name="IsSelectedByDefault"/> is true).</param>
/// <param name="Candidates">All items offered for this line.</param>
/// <param name="Similarity">Best fuzzy similarity, for <see cref="MatchStatus.PossibleFuzzy"/>.</param>
/// <param name="IsSelectedByDefault">Whether the preview checks this row by default.</param>
/// <param name="DuplicateOfLineNumber">Set when an earlier line already matched the same item.</param>
/// <param name="Note">A short human-readable explanation, if useful.</param>
public sealed record MatchResult(
    ParsedLine Line,
    MatchStatus Status,
    Guid? SelectedId,
    IReadOnlyList<MatchCandidate> Candidates,
    double? Similarity,
    bool IsSelectedByDefault,
    int? DuplicateOfLineNumber,
    string? Note);
