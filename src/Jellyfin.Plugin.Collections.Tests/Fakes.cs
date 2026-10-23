using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Collections.Core;
using Jellyfin.Plugin.Collections.Services;

namespace Jellyfin.Plugin.Collections.Tests;

internal sealed class FakeCatalog : ILibraryCatalog
{
    private readonly IReadOnlyList<CatalogEntry> _entries;

    public FakeCatalog(IReadOnlyList<CatalogEntry> entries) => _entries = entries;

    public IReadOnlyList<CatalogEntry> GetEntries(MediaTypes mediaTypes, CancellationToken cancellationToken)
    {
        return mediaTypes switch
        {
            MediaTypes.Movies => [.. _entries.Where(e => e.Kind == ItemKind.Movie)],
            MediaTypes.Shows => [.. _entries.Where(e => e.Kind == ItemKind.Series)],
            _ => _entries
        };
    }

    public IReadOnlyList<CatalogEntry> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return [.. _entries.Where(e => ids.Contains(e.Id))];
    }
}

internal sealed class FakeCollections : ICollectionService
{
    public List<(Guid Id, string Name, HashSet<Guid> Members, bool Locked)> Store { get; } = [];

    public int CreateCalls { get; private set; }

    public int AddCalls { get; private set; }

    public Guid Seed(string name, params Guid[] members)
    {
        var id = Guid.NewGuid();
        Store.Add((id, name, [.. members], false));
        return id;
    }

    public IReadOnlyList<CollectionInfo> GetCollections(CancellationToken cancellationToken)
    {
        return [.. Store.Select(c => new CollectionInfo(c.Id, c.Name, new HashSet<Guid>(c.Members)))];
    }

    public Task<Guid> CreateAsync(string name, IReadOnlyCollection<Guid> itemIds, bool lockMetadata, CancellationToken cancellationToken)
    {
        CreateCalls++;
        var id = Guid.NewGuid();
        Store.Add((id, name, [.. itemIds], lockMetadata));
        return Task.FromResult(id);
    }

    public Task AddAsync(Guid collectionId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        AddCalls++;
        var collection = Store.Single(c => c.Id == collectionId);
        foreach (var id in itemIds)
        {
            if (collection.Members.Contains(id))
            {
                throw new InvalidOperationException("Service must filter existing members before adding.");
            }

            collection.Members.Add(id);
        }

        return Task.CompletedTask;
    }
}
