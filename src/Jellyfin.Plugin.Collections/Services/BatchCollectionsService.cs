using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Collections.Api.Dto;
using Jellyfin.Plugin.Collections.Core;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Runs Preview and Apply. Depends only on small interfaces so it can be tested with fakes.
/// </summary>
public sealed class BatchCollectionsService
{
    /// <summary>
    /// The longest collection name accepted.
    /// </summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// The most item ids accepted by one Apply call.
    /// </summary>
    public const int MaxApplyIds = 20_000;

    private static readonly SemaphoreSlim _applyLock = new(1, 1);

    private readonly ILibraryCatalog _catalog;
    private readonly ICollectionService _collections;
    private readonly ILogger<BatchCollectionsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCollectionsService"/> class.
    /// </summary>
    /// <param name="catalog">The library catalog.</param>
    /// <param name="collections">The collection service.</param>
    /// <param name="logger">The logger.</param>
    public BatchCollectionsService(ILibraryCatalog catalog, ICollectionService collections, ILogger<BatchCollectionsService> logger)
    {
        _catalog = catalog;
        _collections = collections;
        _logger = logger;
    }

    /// <summary>
    /// Gets existing collections, sorted by name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection summaries.</returns>
    public IReadOnlyList<CollectionSummaryDto> GetCollections(CancellationToken cancellationToken)
    {
        return [.. _collections.GetCollections(cancellationToken)
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => new CollectionSummaryDto { Id = c.Id, Name = c.Name, ItemCount = c.MemberIds.Count })];
    }

    /// <summary>
    /// Matches the list against the library. Changes nothing.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview.</returns>
    /// <exception cref="BatchRequestException">The request is invalid.</exception>
    public PreviewResponse Preview(PreviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = ValidateName(request.CollectionName);
        var listError = ListParser.Validate(request.List);
        if (listError is not null)
        {
            throw new BatchRequestException(listError);
        }

        if (request.YearTolerance is < 0 or > 1)
        {
            throw new BatchRequestException("Year tolerance must be 0 (exact) or 1 (±1 year).");
        }

        if (!Enum.IsDefined(request.MediaTypes))
        {
            throw new BatchRequestException("Media type must be Movies, Shows or Both.");
        }

        var parsed = ListParser.Parse(request.List);
        if (parsed.Lines.Count == 0)
        {
            throw new BatchRequestException("The list has no titles (only blank or comment lines).");
        }

        var catalog = _catalog.GetEntries(request.MediaTypes, cancellationToken);
        var target = FindTarget(name, cancellationToken);
        var options = new MatchOptions
        {
            YearTolerance = request.YearTolerance,
            Fuzzy = request.Fuzzy,
            ExistingMemberIds = target.Collection?.MemberIds ?? new HashSet<Guid>()
        };

        var results = Matcher.Match(catalog, parsed.Lines, options, cancellationToken);
        var counts = Enum.GetValues<MatchStatus>().ToDictionary(s => s, s => results.Count(r => r.Status == s));
        _logger.LogInformation(
            "Batch Collections: preview for {Collection}: {Lines} lines, {Matched} matched, {NotFound} not found",
            name,
            results.Count,
            counts[MatchStatus.Matched],
            counts[MatchStatus.NotFound]);

        return new PreviewResponse
        {
            Collection = new TargetCollectionDto
            {
                Name = name,
                Exists = target.Collection is not null,
                Id = target.Collection?.Id,
                ItemCount = target.Collection?.MemberIds.Count ?? 0,
                Warning = target.Warning
            },
            Lines = [.. results.Select(ToDto)],
            DuplicateLines = [.. parsed.Duplicates.Select(d => new DuplicateLineDto
            {
                LineNumber = d.Line.LineNumber,
                Raw = d.Line.Raw,
                DuplicateOfLine = d.DuplicateOfLineNumber
            })],
            Counts = counts,
            LibraryItemCount = catalog.Count,
            IgnoredLineCount = parsed.IgnoredCount
        };
    }

    /// <summary>
    /// Adds exactly the confirmed items to the collection, creating it if needed. Never removes anything.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>What happened.</returns>
    /// <exception cref="BatchRequestException">The request is invalid.</exception>
    public async Task<ApplyResponse> ApplyAsync(ApplyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = ValidateName(request.CollectionName);
        if (request.ItemIds is null || request.ItemIds.Count == 0)
        {
            throw new BatchRequestException("Nothing is selected. Tick at least one row in the preview.");
        }

        if (request.ItemIds.Count > MaxApplyIds)
        {
            throw new BatchRequestException(string.Create(CultureInfo.InvariantCulture, $"Too many items ({request.ItemIds.Count:N0}). The limit is {MaxApplyIds:N0}."));
        }

        var requested = request.ItemIds.Where(id => !id.Equals(Guid.Empty)).Distinct().ToList();
        var valid = _catalog.GetByIds(requested, cancellationToken).Select(e => e.Id).ToHashSet();
        var failed = requested
            .Where(id => !valid.Contains(id))
            .Select(id => new FailedItemDto { Id = id, Reason = "Not a movie or series in the library." })
            .ToList();
        var validInOrder = requested.Where(valid.Contains).ToList();

        await _applyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var target = FindTarget(name, cancellationToken);
            if (target.Collection is { } existing)
            {
                var toAdd = validInOrder.Where(id => !existing.MemberIds.Contains(id)).ToList();
                await _collections.AddAsync(existing.Id, toAdd, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Batch Collections: apply to existing {Collection}: added {Added}, already present {Present}, rejected {Failed}",
                    existing.Name,
                    toAdd.Count,
                    validInOrder.Count - toAdd.Count,
                    failed.Count);
                return new ApplyResponse
                {
                    CollectionId = existing.Id,
                    CollectionName = existing.Name,
                    Created = false,
                    Added = toAdd.Count,
                    AlreadyPresent = validInOrder.Count - toAdd.Count,
                    Failed = failed,
                    Warning = target.Warning
                };
            }

            if (validInOrder.Count == 0)
            {
                throw new BatchRequestException("None of the selected items are movies or series in the library, so no collection was created.");
            }

            var id = await _collections.CreateAsync(name, validInOrder, request.LockMetadata, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Batch Collections: created {Collection} with {Added} items, rejected {Failed}",
                name,
                validInOrder.Count,
                failed.Count);
            return new ApplyResponse
            {
                CollectionId = id,
                CollectionName = name,
                Created = true,
                Added = validInOrder.Count,
                AlreadyPresent = 0,
                Failed = failed
            };
        }
        finally
        {
            _applyLock.Release();
        }
    }

    private static string ValidateName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new BatchRequestException("Enter a collection name.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new BatchRequestException(string.Create(CultureInfo.InvariantCulture, $"The collection name is too long (limit {MaxNameLength} characters)."));
        }

        return trimmed;
    }

    private static PreviewLineDto ToDto(MatchResult result)
    {
        return new PreviewLineDto
        {
            LineNumber = result.Line.LineNumber,
            Raw = result.Line.Raw,
            Title = result.Line.Title,
            Year = result.Line.Year,
            Status = result.Status,
            SelectedId = result.SelectedId,
            Selected = result.IsSelectedByDefault,
            Similarity = result.Similarity,
            DuplicateOfLine = result.DuplicateOfLineNumber,
            Note = result.Note,
            Candidates = [.. result.Candidates.Select(c => new CandidateDto
            {
                Id = c.Entry.Id,
                Name = c.Entry.Name,
                Year = c.Entry.Year,
                Kind = c.Entry.Kind,
                LibraryName = c.Entry.LibraryName,
                InCollection = c.InCollection,
                Similarity = c.Similarity
            })]
        };
    }

    private (CollectionInfo? Collection, string? Warning) FindTarget(string name, CancellationToken cancellationToken)
    {
        var matches = _collections.GetCollections(cancellationToken)
            .Where(c => string.Equals(c.Name.Trim(), name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.MemberIds.Count)
            .ToList();

        if (matches.Count == 0)
        {
            return (null, null);
        }

        var warning = matches.Count > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{matches.Count} collections are named \"{name}\". Using the largest one ({matches[0].MemberIds.Count} items).")
            : null;
        return (matches[0], warning);
    }
}
