namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// A list line that repeats an earlier one.
/// </summary>
/// <param name="Line">The repeated line.</param>
/// <param name="DuplicateOfLineNumber">The line number of the first occurrence.</param>
public sealed record DuplicateLine(ParsedLine Line, int DuplicateOfLineNumber);
