using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// The result of parsing a pasted list.
/// </summary>
/// <param name="Lines">Unique usable lines, in list order.</param>
/// <param name="Duplicates">Lines dropped because an earlier line had the same title and year.</param>
/// <param name="IgnoredCount">Blank and comment lines that were skipped.</param>
public sealed record ParseResult(
    IReadOnlyList<ParsedLine> Lines,
    IReadOnlyList<DuplicateLine> Duplicates,
    int IgnoredCount);
