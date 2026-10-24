using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Collections.Api.Dto;
using Jellyfin.Plugin.Collections.Core;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// Sends titles that aren't in the library to Seerr as requests.
/// </summary>
public sealed class SeerrService : IDisposable
{
    /// <summary>
    /// The most titles accepted by one request call.
    /// </summary>
    public const int MaxItems = 50;

    private const int MaxCandidates = 10;
    private const string Movie = "movie";
    private const string Tv = "tv";

    private readonly ISeerrSettings _settings;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly ILogger<SeerrService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeerrService"/> class.
    /// </summary>
    /// <param name="settings">The Seerr settings.</param>
    /// <param name="logger">The logger.</param>
    public SeerrService(ISeerrSettings settings, ILogger<SeerrService> logger)
        : this(settings, new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }), logger, ownsHttp: true)
    {
    }

    internal SeerrService(ISeerrSettings settings, HttpClient http, ILogger<SeerrService> logger, bool ownsHttp = false)
    {
        _settings = settings;
        _http = http;
        _ownsHttp = ownsHttp;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Checks that Seerr answers and accepts the API key.
    /// </summary>
    /// <param name="request">Values to test; blank values use the saved settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result.</returns>
    public async Task<SeerrTestResponse> TestAsync(SeerrTestRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = string.IsNullOrWhiteSpace(request.Url) ? _settings.Url : request.Url;
        var key = string.IsNullOrWhiteSpace(request.ApiKey) ? _settings.ApiKey : request.ApiKey;
        if (!TryGetBaseUri(url, out var baseUri))
        {
            return new SeerrTestResponse { Message = "Enter the Seerr address, for example http://192.168.1.10:5055." };
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return new SeerrTestResponse { Message = "Enter the Seerr API key (Seerr: Settings, General)." };
        }

        try
        {
            string? version = null;
            using (var status = await SendAsync(baseUri, key, HttpMethod.Get, "status", null, cancellationToken).ConfigureAwait(false))
            {
                if (status.IsSuccessStatusCode)
                {
                    using var doc = await ReadJsonAsync(status, cancellationToken).ConfigureAwait(false);
                    version = doc is not null && doc.RootElement.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                }
            }

            using var me = await SendAsync(baseUri, key, HttpMethod.Get, "auth/me", null, cancellationToken).ConfigureAwait(false);
            if (me.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new SeerrTestResponse { Message = "Seerr answered, but rejected the API key." };
            }

            if (!me.IsSuccessStatusCode)
            {
                return new SeerrTestResponse { Message = string.Create(CultureInfo.InvariantCulture, $"Seerr answered with HTTP {(int)me.StatusCode}. Check the address.") };
            }

            return new SeerrTestResponse { Ok = true, Message = version is null ? "Connected to Seerr." : "Connected to Seerr " + version + "." };
        }
        catch (Exception ex) when (IsTransportError(ex, cancellationToken))
        {
            return new SeerrTestResponse { Message = "Could not reach Seerr at " + baseUri + " from the Jellyfin server (" + ex.Message + ")." };
        }
    }

    /// <summary>
    /// Requests each title in Seerr, one after another.
    /// </summary>
    /// <param name="items">The titles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One result per title, in order.</returns>
    /// <exception cref="BatchRequestException">Seerr isn't set up, or the request is invalid.</exception>
    public async Task<IReadOnlyList<SeerrResultDto>> RequestAsync(IReadOnlyList<SeerrRequestItem>? items, CancellationToken cancellationToken)
    {
        if (!TryGetBaseUri(_settings.Url, out var baseUri) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new BatchRequestException("Seerr isn't set up. Enter its address and API key under Seerr and click Save.");
        }

        if (items is null || items.Count == 0)
        {
            throw new BatchRequestException("There is nothing to request.");
        }

        if (items.Count > MaxItems)
        {
            throw new BatchRequestException(string.Create(CultureInfo.InvariantCulture, $"Too many titles in one call ({items.Count}). The limit is {MaxItems}."));
        }

        var key = _settings.ApiKey.Trim();
        var results = new List<SeerrResultDto>(items.Count);
        string? unreachable = null;
        foreach (var item in items)
        {
            if (unreachable is not null)
            {
                results.Add(new SeerrResultDto { LineNumber = item.LineNumber, Outcome = SeerrOutcome.Failed, Message = unreachable });
                continue;
            }

            try
            {
                results.Add(await RequestOneAsync(baseUri, key, item, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex) when (IsTransportError(ex, cancellationToken))
            {
                _logger.LogWarning(ex, "Paste Collections: Seerr at {Url} could not be reached", baseUri);
                unreachable = "Could not reach Seerr from the Jellyfin server (" + ex.Message + ").";
                results.Add(new SeerrResultDto { LineNumber = item.LineNumber, Outcome = SeerrOutcome.Failed, Message = unreachable });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    /// <summary>
    /// Picks the one search result that clearly matches the title (and year, when given).
    /// </summary>
    /// <param name="candidates">Search results of the wanted kinds.</param>
    /// <param name="title">The list title.</param>
    /// <param name="year">The list year.</param>
    /// <returns>The match, or <c>null</c> when it isn't clear.</returns>
    internal static SeerrCandidateDto? Pick(IReadOnlyList<SeerrCandidateDto> candidates, string title, int? year)
    {
        var strict = TitleNormalizer.Strict(title);
        var loose = TitleNormalizer.Loose(title);
        var sameTitle = candidates
            .Where(c => string.Equals(TitleNormalizer.Strict(c.Title), strict, StringComparison.Ordinal)
                || string.Equals(TitleNormalizer.Loose(c.Title), loose, StringComparison.Ordinal))
            .ToList();
        if (year is not null)
        {
            sameTitle = [.. sameTitle.Where(c => c.Year == year)];
        }

        return sameTitle.Count == 1 ? sameTitle[0] : null;
    }

    internal static bool TryGetBaseUri(string? url, out Uri baseUri)
    {
        baseUri = null!;
        var text = (url ?? string.Empty).Trim().TrimEnd('/');
        if (text.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^"/api/v1".Length];
        }

        if (!Uri.TryCreate(text + "/", UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        baseUri = uri;
        return true;
    }

    private static bool IsTransportError(Exception ex, CancellationToken cancellationToken)
    {
        return ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    }

    private static bool Accepts(MediaTypes mediaTypes, string mediaType) => mediaTypes switch
    {
        MediaTypes.Movies => mediaType == Movie,
        MediaTypes.Shows => mediaType == Tv,
        _ => mediaType is Movie or Tv
    };

    private static IEnumerable<SeerrCandidateDto> ParseSearch(JsonDocument? doc)
    {
        if (doc is null || !doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var r in results.EnumerateArray())
        {
            var mediaType = GetString(r, "mediaType");
            if ((mediaType != Movie && mediaType != Tv) || !r.TryGetProperty("id", out var id) || !id.TryGetInt32(out var tmdbId))
            {
                continue;
            }

            var date = GetString(r, mediaType == Movie ? "releaseDate" : "firstAirDate");
            int? year = date is { Length: >= 4 } && int.TryParse(date.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var y) ? y : null;
            var status = r.TryGetProperty("mediaInfo", out var info) && info.ValueKind == JsonValueKind.Object
                && info.TryGetProperty("status", out var s) && s.TryGetInt32(out var st) ? st : 0;
            yield return new SeerrCandidateDto
            {
                TmdbId = tmdbId,
                MediaType = mediaType,
                Title = GetString(r, mediaType == Movie ? "title" : "name") ?? string.Empty,
                Year = year,
                Status = status
            };
        }
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static async Task<JsonDocument?> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        var message = doc is not null && doc.RootElement.ValueKind == JsonValueKind.Object ? GetString(doc.RootElement, "message") : null;
        return string.IsNullOrWhiteSpace(message)
            ? string.Create(CultureInfo.InvariantCulture, $"Seerr answered with HTTP {(int)response.StatusCode}.")
            : "Seerr: " + message;
    }

    private static string Describe(SeerrCandidateDto c)
    {
        return c.Year is null ? c.Title : string.Create(CultureInfo.InvariantCulture, $"{c.Title} ({c.Year})");
    }

    private async Task<HttpResponseMessage> SendAsync(Uri baseUri, string key, HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, new Uri(baseUri, "api/v1/" + path));
        message.Headers.Add("X-Api-Key", key.Trim());
        if (body is not null)
        {
            message.Content = JsonContent.Create(body);
        }

        return await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SeerrResultDto> RequestOneAsync(Uri baseUri, string key, SeerrRequestItem item, CancellationToken cancellationToken)
    {
        var title = item.Title?.Trim() ?? string.Empty;
        if (item.TmdbId is > 0 && item.MediaType is Movie or Tv)
        {
            var chosen = new SeerrCandidateDto { TmdbId = item.TmdbId.Value, MediaType = item.MediaType, Title = title, Year = item.Year };
            return await SubmitAsync(baseUri, key, item.LineNumber, chosen, cancellationToken).ConfigureAwait(false);
        }

        if (title.Length == 0)
        {
            return new SeerrResultDto { LineNumber = item.LineNumber, Outcome = SeerrOutcome.Failed, Message = "The line has no title." };
        }

        List<SeerrCandidateDto> candidates;
        using (var response = await SendAsync(baseUri, key, HttpMethod.Get, "search?page=1&query=" + Uri.EscapeDataString(title), null, cancellationToken).ConfigureAwait(false))
        {
            if (!response.IsSuccessStatusCode)
            {
                return new SeerrResultDto { LineNumber = item.LineNumber, Outcome = SeerrOutcome.Failed, Message = await ErrorMessageAsync(response, cancellationToken).ConfigureAwait(false) };
            }

            using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            candidates = [.. ParseSearch(doc).Where(c => Accepts(item.MediaTypes, c.MediaType))];
        }

        if (candidates.Count == 0)
        {
            return new SeerrResultDto { LineNumber = item.LineNumber, Outcome = SeerrOutcome.NotFound, Message = "Seerr found nothing for \"" + title + "\"." };
        }

        var pick = Pick(candidates, title, item.Year);
        if (pick is null)
        {
            return new SeerrResultDto
            {
                LineNumber = item.LineNumber,
                Outcome = SeerrOutcome.NeedsChoice,
                Message = "Seerr has several possible matches. Pick one.",
                Candidates = [.. candidates.OrderByDescending(c => item.Year is not null && c.Year == item.Year).Take(MaxCandidates)]
            };
        }

        return await SubmitAsync(baseUri, key, item.LineNumber, pick, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SeerrResultDto> SubmitAsync(Uri baseUri, string key, int lineNumber, SeerrCandidateDto item, CancellationToken cancellationToken)
    {
        if (item.Status is 4 or 5)
        {
            return new SeerrResultDto { LineNumber = lineNumber, Outcome = SeerrOutcome.AlreadyAvailable, Item = item, Message = Describe(item) + " is already available in Seerr." };
        }

        if (item.Status is 2 or 3)
        {
            return new SeerrResultDto { LineNumber = lineNumber, Outcome = SeerrOutcome.AlreadyRequested, Item = item, Message = Describe(item) + " is already requested." };
        }

        object body = item.MediaType == Tv
            ? new { mediaType = Tv, mediaId = item.TmdbId, seasons = "all" }
            : new { mediaType = Movie, mediaId = item.TmdbId };
        using var response = await SendAsync(baseUri, key, HttpMethod.Post, "request", body, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Paste Collections: requested {MediaType} {TmdbId} in Seerr", item.MediaType, item.TmdbId);
            return new SeerrResultDto { LineNumber = lineNumber, Outcome = SeerrOutcome.Requested, Item = item, Message = "Requested " + Describe(item) + "." };
        }

        var error = await ErrorMessageAsync(response, cancellationToken).ConfigureAwait(false);
        return new SeerrResultDto
        {
            LineNumber = lineNumber,
            Outcome = response.StatusCode == HttpStatusCode.Conflict ? SeerrOutcome.AlreadyRequested : SeerrOutcome.Failed,
            Item = item,
            Message = error
        };
    }
}
