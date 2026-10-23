using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Wraps <see cref="ICollectionManager"/>. Only uses the official collection APIs; never touches linked-children storage directly.
/// </summary>
public sealed class CollectionService : ICollectionService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<CollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="collectionManager">The collection manager.</param>
    /// <param name="logger">The logger.</param>
    public CollectionService(ILibraryManager libraryManager, ICollectionManager collectionManager, ILogger<CollectionService> logger)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<CollectionInfo> GetCollections(CancellationToken cancellationToken)
    {
        // A BoxSet query loads each collection's linked children (12.x stores them as rows).
        var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            Recursive = true,
            DtoOptions = DtoOptions.StoredColumnsOnly
        });

        var collections = new List<CollectionInfo>(boxSets.Count);
        foreach (var boxSet in boxSets.OfType<BoxSet>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            collections.Add(new CollectionInfo(boxSet.Id, boxSet.Name ?? string.Empty, GetMemberIds(boxSet)));
        }

        return collections;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(string name, IReadOnlyCollection<Guid> itemIds, bool lockMetadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        cancellationToken.ThrowIfCancellationRequested();

        var collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
        {
            Name = name,
            IsLocked = lockMetadata,
            ItemIdList = [.. itemIds.Select(id => id.ToString("N", CultureInfo.InvariantCulture))]
        }).ConfigureAwait(false);

        _logger.LogInformation("Batch Collections: created collection {Name} ({Id}) with {Count} items", name, collection.Id, itemIds.Count);
        return collection.Id;
    }

    /// <inheritdoc />
    public async Task AddAsync(Guid collectionId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        if (itemIds.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _collectionManager.AddToCollectionAsync(collectionId, itemIds).ConfigureAwait(false);
        _logger.LogInformation("Batch Collections: added {Count} items to collection {Id}", itemIds.Count, collectionId);
    }

    private static HashSet<Guid> GetMemberIds(BoxSet boxSet)
    {
        var ids = new HashSet<Guid>();
        var needsResolve = false;
        foreach (var link in boxSet.LinkedChildren)
        {
            if (link.ItemId is { } id && !id.Equals(Guid.Empty))
            {
                ids.Add(id);
            }
            else
            {
                needsResolve = true;
            }
        }

        if (needsResolve || !boxSet.LinkedChildrenLoaded)
        {
            // Legacy path-based links (or an unloaded instance): resolve through the supported API.
            foreach (var child in boxSet.GetLinkedChildren(DtoOptions.StoredColumnsOnly))
            {
                ids.Add(child.Id);
            }
        }

        return ids;
    }
}
