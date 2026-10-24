using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Collections.Api.Dto;
using Jellyfin.Plugin.Collections.Core;
using Jellyfin.Plugin.Collections.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.Collections.Tests;

public sealed class SeerrServiceTests : IDisposable
{
    private const string LionKingSearch = """
        { "results": [
            { "id": 8587, "mediaType": "movie", "title": "The Lion King", "releaseDate": "1994-06-15" },
            { "id": 420818, "mediaType": "movie", "title": "The Lion King", "releaseDate": "2019-07-12" },
            { "id": 9732, "mediaType": "movie", "title": "The Lion King 1½", "releaseDate": "2004-02-10" },
            { "id": 1, "mediaType": "person", "name": "Lion King" }
        ] }
        """;

    private readonly FakeSeerr _seerr = new();
    private readonly SeerrService _service;

    public SeerrServiceTests()
    {
        _service = new SeerrService(new FakeSettings("http://seerr:5055", "key"), new HttpClient(_seerr), NullLogger<SeerrService>.Instance);
    }

    public void Dispose()
    {
        _service.Dispose();
        _seerr.Dispose();
    }

    private static SeerrRequestItem Item(string title, int? year = null, MediaTypes types = MediaTypes.Both, int line = 1)
    {
        return new SeerrRequestItem { LineNumber = line, Title = title, Year = year, MediaTypes = types };
    }

    private Task<IReadOnlyList<SeerrResultDto>> Request(params SeerrRequestItem[] items) => _service.RequestAsync(items, CancellationToken.None);

    [Fact]
    public async Task Request_YearPicksTheRightRemake_AndPostsIt()
    {
        _seerr.Search = LionKingSearch;

        var result = Assert.Single(await Request(Item("The Lion King", 2019)));

        Assert.Equal(SeerrOutcome.Requested, result.Outcome);
        Assert.Equal(420818, result.Item!.TmdbId);
        var post = Assert.Single(_seerr.Posts);
        Assert.Contains("\"mediaType\":\"movie\"", post, StringComparison.Ordinal);
        Assert.Contains("\"mediaId\":420818", post, StringComparison.Ordinal);
        Assert.Equal("key", _seerr.LastApiKey);
    }

    [Fact]
    public async Task Request_SameTitleNoYear_AsksToChoose()
    {
        _seerr.Search = LionKingSearch;

        var result = Assert.Single(await Request(Item("The Lion King")));

        Assert.Equal(SeerrOutcome.NeedsChoice, result.Outcome);
        Assert.Equal(3, result.Candidates.Count);
        Assert.Empty(_seerr.Posts);
    }

    [Fact]
    public async Task Request_SeriesRequestsAllSeasons()
    {
        _seerr.Search = """{ "results": [ { "id": 1399, "mediaType": "tv", "name": "Game of Thrones", "firstAirDate": "2011-04-17" } ] }""";

        var result = Assert.Single(await Request(Item("Game of Thrones", types: MediaTypes.Shows)));

        Assert.Equal(SeerrOutcome.Requested, result.Outcome);
        var post = Assert.Single(_seerr.Posts);
        Assert.Contains("\"mediaType\":\"tv\"", post, StringComparison.Ordinal);
        Assert.Contains("\"seasons\":\"all\"", post, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_MoviesOnly_IgnoresSeries()
    {
        _seerr.Search = """{ "results": [ { "id": 1, "mediaType": "tv", "name": "Moana", "firstAirDate": "2020-01-01" } ] }""";

        var result = Assert.Single(await Request(Item("Moana", types: MediaTypes.Movies)));

        Assert.Equal(SeerrOutcome.NotFound, result.Outcome);
    }

    [Theory]
    [InlineData(5, SeerrOutcome.AlreadyAvailable)]
    [InlineData(4, SeerrOutcome.AlreadyAvailable)]
    [InlineData(2, SeerrOutcome.AlreadyRequested)]
    public async Task Request_KnownToSeerr_DoesNotPost(int status, SeerrOutcome expected)
    {
        _seerr.Search = "{ \"results\": [ { \"id\": 277834, \"mediaType\": \"movie\", \"title\": \"Moana\", \"releaseDate\": \"2016-11-23\", \"mediaInfo\": { \"status\": " + status + " } } ] }";

        var result = Assert.Single(await Request(Item("Moana", 2016)));

        Assert.Equal(expected, result.Outcome);
        Assert.Empty(_seerr.Posts);
    }

    [Fact]
    public async Task Request_Conflict_IsAlreadyRequested()
    {
        _seerr.Search = """{ "results": [ { "id": 277834, "mediaType": "movie", "title": "Moana", "releaseDate": "2016-11-23" } ] }""";
        _seerr.PostStatus = HttpStatusCode.Conflict;
        _seerr.PostBody = """{ "message": "Request for this media already exists." }""";

        var result = Assert.Single(await Request(Item("Moana")));

        Assert.Equal(SeerrOutcome.AlreadyRequested, result.Outcome);
        Assert.Contains("already exists", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_ChosenTmdbId_SkipsSearch()
    {
        var result = Assert.Single(await Request(new SeerrRequestItem { LineNumber = 7, Title = "The Lion King", Year = 1994, TmdbId = 8587, MediaType = "movie" }));

        Assert.Equal(SeerrOutcome.Requested, result.Outcome);
        Assert.Equal(7, result.LineNumber);
        Assert.Equal(0, _seerr.Searches);
        Assert.Contains("\"mediaId\":8587", Assert.Single(_seerr.Posts), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_Unreachable_FailsTheRestWithoutRetrying()
    {
        _seerr.Throw = true;

        var results = await Request(Item("Moana", line: 1), Item("Coco", line: 2), Item("Up", line: 3));

        Assert.All(results, r => Assert.Equal(SeerrOutcome.Failed, r.Outcome));
        Assert.Equal([1, 2, 3], results.Select(r => r.LineNumber));
        Assert.Equal(1, _seerr.Searches);
    }

    [Fact]
    public async Task Request_NotConfigured_IsReadableError()
    {
        using var service = new SeerrService(new FakeSettings(string.Empty, string.Empty), new HttpClient(_seerr), NullLogger<SeerrService>.Instance);

        await Assert.ThrowsAsync<BatchRequestException>(() => service.RequestAsync([Item("Moana")], CancellationToken.None));
    }

    [Fact]
    public async Task Request_TooMany_IsRejected()
    {
        var items = Enumerable.Range(1, SeerrService.MaxItems + 1).Select(i => Item("Moana", line: i)).ToArray();

        await Assert.ThrowsAsync<BatchRequestException>(() => Request(items));
    }

    [Fact]
    public async Task Test_BadKey_ReportsIt()
    {
        _seerr.AuthStatus = HttpStatusCode.Forbidden;

        var result = await _service.TestAsync(new SeerrTestRequest(), CancellationToken.None);

        Assert.False(result.Ok);
        Assert.Contains("API key", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Test_Good_ReportsVersion()
    {
        var result = await _service.TestAsync(new SeerrTestRequest(), CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Contains("2.7.3", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://seerr:5055", "http://seerr:5055/")]
    [InlineData(" http://seerr:5055/api/v1/ ", "http://seerr:5055/")]
    [InlineData("https://example.com/seerr", "https://example.com/seerr/")]
    public void BaseUri_IsNormalised(string input, string expected)
    {
        Assert.True(SeerrService.TryGetBaseUri(input, out var uri));
        Assert.Equal(expected, uri.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("seerr:5055")]
    [InlineData("ftp://seerr")]
    public void BaseUri_RejectsJunk(string input)
    {
        Assert.False(SeerrService.TryGetBaseUri(input, out _));
    }

    private sealed class FakeSettings(string url, string apiKey) : ISeerrSettings
    {
        public string Url => url;

        public string ApiKey => apiKey;
    }

    private sealed class FakeSeerr : HttpMessageHandler
    {
        public string Search { get; set; } = """{ "results": [] }""";

        public HttpStatusCode PostStatus { get; set; } = HttpStatusCode.Created;

        public string PostBody { get; set; } = "{}";

        public HttpStatusCode AuthStatus { get; set; } = HttpStatusCode.OK;

        public bool Throw { get; set; }

        public int Searches { get; private set; }

        public List<string> Posts { get; } = [];

        public string? LastApiKey { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastApiKey = request.Headers.TryGetValues("X-Api-Key", out var keys) ? keys.Single() : null;
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/search", StringComparison.Ordinal))
            {
                Searches++;
            }

            if (Throw)
            {
                throw new HttpRequestException("Connection refused");
            }

            if (path.EndsWith("/status", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{ "version": "2.7.3" }""");
            }

            if (path.EndsWith("/auth/me", StringComparison.Ordinal))
            {
                return Json(AuthStatus, """{ "id": 1 }""");
            }

            if (path.EndsWith("/search", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, Search);
            }

            if (path.EndsWith("/request", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                Posts.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
                return Json(PostStatus, PostBody);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body)
        {
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
