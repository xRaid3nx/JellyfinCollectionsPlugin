namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// One library item offered for a list line.
/// </summary>
/// <param name="Entry">The library item.</param>
/// <param name="InCollection">Whether the item is already in the target collection.</param>
/// <param name="Similarity">Fuzzy similarity (0-1) when found by fuzzy matching; otherwise null.</param>
public sealed record MatchCandidate(CatalogEntry Entry, bool InCollection, double? Similarity = null);
