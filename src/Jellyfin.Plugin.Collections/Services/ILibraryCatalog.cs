using System;
using System.Collections.Generic;
using System.Threading;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Reads library items as <see cref="CatalogEntry"/> values.
/// </summary>
public interface ILibraryCatalog
{
    /// <summary>
    /// Gets every real (non-virtual, primary-version) movie and/or series in the server, regardless of user.
    /// </summary>
    /// <param name="mediaTypes">Which kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The catalog.</returns>
    IReadOnlyList<CatalogEntry> GetEntries(MediaTypes mediaTypes, CancellationToken cancellationToken);

    /// <summary>
    /// Looks up the given ids and returns only those that are real movies or series.
    /// </summary>
    /// <param name="ids">The ids to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The valid entries.</returns>
    IReadOnlyList<CatalogEntry> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
