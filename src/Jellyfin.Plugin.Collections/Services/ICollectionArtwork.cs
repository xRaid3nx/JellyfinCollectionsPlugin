using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Gives a newly created collection a poster.
/// </summary>
public interface ICollectionArtwork
{
    /// <summary>
    /// Identifies the collection on TMDB and, if allowed, falls back to a collage of its members' posters.
    /// </summary>
    /// <param name="collectionId">The new collection.</param>
    /// <param name="name">The collection name.</param>
    /// <param name="memberIds">The members, in list order (the collage uses the first ones).</param>
    /// <param name="mode">The poster mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>What happened.</returns>
    Task<PosterResult> AddPosterAsync(Guid collectionId, string name, IReadOnlyList<Guid> memberIds, PosterMode mode, CancellationToken cancellationToken);

    /// <summary>
    /// Locks the collection's metadata.
    /// </summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task LockAsync(Guid collectionId, CancellationToken cancellationToken);
}
