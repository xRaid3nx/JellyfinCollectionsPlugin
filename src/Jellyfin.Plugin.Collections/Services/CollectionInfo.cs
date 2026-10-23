using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// A collection (BoxSet) and its member ids.
/// </summary>
/// <param name="Id">The collection id.</param>
/// <param name="Name">The collection name.</param>
/// <param name="MemberIds">The ids of the items in the collection.</param>
public sealed record CollectionInfo(Guid Id, string Name, IReadOnlySet<Guid> MemberIds);
