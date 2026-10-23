namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// The outcome of matching one list line against the library.
/// </summary>
public enum MatchStatus
{
    /// <summary>
    /// Exactly one item matched by title (and year, when given).
    /// </summary>
    Matched,

    /// <summary>
    /// Matched, but the item is already in the target collection.
    /// </summary>
    AlreadyInCollection,

    /// <summary>
    /// Several items share the title; the user must choose.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// The title matches but the year is off by exactly one.
    /// </summary>
    PossibleYearOffByOne,

    /// <summary>
    /// No exact title match, but a close spelling was found.
    /// </summary>
    PossibleFuzzy,

    /// <summary>
    /// The title matches but the year is clearly different.
    /// </summary>
    YearMismatch,

    /// <summary>
    /// Nothing acceptable was found.
    /// </summary>
    NotFound
}
