using System;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// A library item offered for a list line.
/// </summary>
public sealed class CandidateDto
{
    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the item kind.
    /// </summary>
    public ItemKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the library name.
    /// </summary>
    public string? LibraryName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is already in the target collection.
    /// </summary>
    public bool InCollection { get; set; }

    /// <summary>
    /// Gets or sets the fuzzy similarity, if any.
    /// </summary>
    public double? Similarity { get; set; }
}
