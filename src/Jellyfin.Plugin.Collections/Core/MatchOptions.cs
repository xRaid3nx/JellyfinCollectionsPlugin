using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// Options that control matching.
/// </summary>
public sealed class MatchOptions
{
    /// <summary>
    /// The minimum similarity for a fuzzy match.
    /// </summary>
    public const double DefaultFuzzyThreshold = 0.90;

    /// <summary>
    /// Gets the year tolerance: 0 = exact year only, 1 = treat off-by-one as a possible match.
    /// </summary>
    public int YearTolerance { get; init; }

    /// <summary>
    /// Gets a value indicating whether near-miss titles are offered.
    /// </summary>
    public bool Fuzzy { get; init; } = true;

    /// <summary>
    /// Gets the minimum fuzzy similarity (0-1).
    /// </summary>
    public double FuzzyThreshold { get; init; } = DefaultFuzzyThreshold;

    /// <summary>
    /// Gets the ids already in the target collection.
    /// </summary>
    public IReadOnlySet<Guid> ExistingMemberIds { get; init; } = new HashSet<Guid>();
}
