using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Tests;

public class MatcherTests
{
    private static MatchResult MatchOne(string line, MatchOptions? options = null, IReadOnlyList<CatalogEntry>? catalog = null)
    {
        var parsed = ListParser.Parse(line, 2026);
        Assert.Single(parsed.Lines);
        return Assert.Single(Matcher.Match(catalog ?? DisneyCatalog.All, parsed.Lines, options ?? new MatchOptions()));
    }

    [Theory]
    [InlineData("The Lion King (1994)", "The Lion King", 1994)]
    [InlineData("The Lion King (2019)", "The Lion King", 2019)]
    [InlineData("Lion King, The (1994)", "The Lion King", 1994)]
    [InlineData("Aladdin (1992)", "Aladdin", 1992)]
    [InlineData("Aladdin (2019)", "Aladdin", 2019)]
    [InlineData("Beauty and the Beast (1991)", "Beauty and the Beast", 1991)]
    [InlineData("Beauty and the Beast (2017)", "Beauty and the Beast", 2017)]
    [InlineData("Cinderella (1950)", "Cinderella", 1950)]
    [InlineData("Cinderella (2015)", "Cinderella", 2015)]
    [InlineData("Dumbo (1941)", "Dumbo", 1941)]
    [InlineData("Dumbo (2019)", "Dumbo", 2019)]
    [InlineData("The Jungle Book (1967)", "The Jungle Book", 1967)]
    [InlineData("The Jungle Book (2016)", "The Jungle Book", 2016)]
    [InlineData("Mulan (1998)", "Mulan", 1998)]
    [InlineData("Mulan (2020)", "Mulan", 2020)]
    [InlineData("Lilo & Stitch (2002)", "Lilo & Stitch", 2002)]
    [InlineData("Lilo and Stitch (2025)", "Lilo & Stitch", 2025)]
    public void RemakePairs_WithYear_PickTheRightOne(string line, string name, int year)
    {
        var result = MatchOne(line);

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get(name, year).Id, result.SelectedId);
        Assert.True(result.IsSelectedByDefault);
    }

    [Theory]
    [InlineData("The Lion King")]
    [InlineData("Aladdin")]
    [InlineData("Beauty and the Beast")]
    [InlineData("Cinderella")]
    [InlineData("Dumbo")]
    [InlineData("The Jungle Book")]
    [InlineData("Mulan")]
    [InlineData("Lilo & Stitch")]
    public void RemakePairs_WithoutYear_AreAmbiguous(string line)
    {
        var result = MatchOne(line);

        Assert.Equal(MatchStatus.Ambiguous, result.Status);
        Assert.Null(result.SelectedId);
        Assert.False(result.IsSelectedByDefault);
        Assert.Equal(2, result.Candidates.Count);
        Assert.True(result.Candidates[0].Entry.Year < result.Candidates[1].Entry.Year);
    }

    [Theory]
    [InlineData("Little Mermaid, The", "The Little Mermaid", 1989)]
    [InlineData("Frozen 2", "Frozen II", 2019)]
    [InlineData("Frozen II", "Frozen II", 2019)]
    [InlineData("WALL·E", "WALL-E", 2008)]
    [InlineData("WALL-E", "WALL-E", 2008)]
    [InlineData("Wreck It Ralph", "Wreck-It Ralph", 2012)]
    [InlineData("Tron Legacy", "Tron: Legacy", 2010)]
    [InlineData("Disney's A Christmas Carol", "A Christmas Carol", 2009)]
    [InlineData("Pirates of the Caribbean The Curse of the Black Pearl", "Pirates of the Caribbean: The Curse of the Black Pearl", 2003)]
    [InlineData("Spider-Man: Into the Spider-Verse", "Spider-Man: Into the Spider-Verse", 2018)]
    [InlineData("Spider-Man Into the Spider-Verse (2018)", "Spider-Man: Into the Spider-Verse", 2018)]
    [InlineData("Amelie", "Amélie", 2001)]
    [InlineData("Le Fabuleux Destin d'Amelie Poulain", "Amélie", 2001)]
    [InlineData("Monsters Inc", "Monsters, Inc.", 2001)]
    [InlineData("Big Hero Six", "Big Hero 6", 2014)]
    [InlineData("Toy Story III (2010)", "Toy Story 3", 2010)]
    [InlineData("A Bugs Life", "A Bug's Life", 1998)]
    public void NameVariants_Match(string line, string name, int year)
    {
        var result = MatchOne(line, catalog: DisneyCatalog.MoviesOnly);

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get(name, year).Id, result.SelectedId);
    }

    [Fact]
    public void Dalmatians_NumberBeyondTen_IsNotMatchedExactly()
    {
        var result = MatchOne("101 Dalmatians", new MatchOptions { Fuzzy = false });

        Assert.Equal(MatchStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("2012", "2012", 2009)]
    [InlineData("1917", "1917", 2019)]
    [InlineData("300", "300", 2007)]
    [InlineData("Blade Runner 2049", "Blade Runner 2049", 2017)]
    [InlineData("Fantasia 2000", "Fantasia 2000", 1999)]
    [InlineData("1917 (2019)", "1917", 2019)]
    public void NumericTitles_Match(string line, string name, int year)
    {
        var result = MatchOne(line);

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get(name, year).Id, result.SelectedId);
    }

    [Fact]
    public void BareTrailingYear_FallsBackToYear()
    {
        var result = MatchOne("Aladdin 1992");

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get("Aladdin", 1992).Id, result.SelectedId);
    }

    [Fact]
    public void YearGiven_ExactWins_OverEverything()
    {
        var result = MatchOne("Mulan (1998)", new MatchOptions { YearTolerance = 1 });

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get("Mulan", 1998).Id, result.SelectedId);
    }

    [Fact]
    public void OffByOneYear_WithTolerance_IsPossibleAndUnchecked()
    {
        var result = MatchOne("Moana (2017)", new MatchOptions { YearTolerance = 1 });

        Assert.Equal(MatchStatus.PossibleYearOffByOne, result.Status);
        Assert.Equal(DisneyCatalog.Get("Moana", 2016).Id, result.SelectedId);
        Assert.False(result.IsSelectedByDefault);
    }

    [Fact]
    public void OffByOneYear_WithoutTolerance_IsYearMismatch()
    {
        var result = MatchOne("Moana (2017)");

        Assert.Equal(MatchStatus.YearMismatch, result.Status);
        Assert.False(result.IsSelectedByDefault);
        Assert.Contains("2016", result.Note, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearlyDifferentYear_IsYearMismatch()
    {
        var result = MatchOne("The Lion King (2005)", new MatchOptions { YearTolerance = 1 });

        Assert.Equal(MatchStatus.YearMismatch, result.Status);
        Assert.Null(result.SelectedId);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void Fuzzy_FindsTypos_Unchecked_WithScore()
    {
        var result = MatchOne("The Hunchback of Notre Damme");

        Assert.Equal(MatchStatus.PossibleFuzzy, result.Status);
        Assert.Equal(DisneyCatalog.Get("The Hunchback of Notre Dame", 1996).Id, result.SelectedId);
        Assert.False(result.IsSelectedByDefault);
        Assert.NotNull(result.Similarity);
        Assert.True(result.Similarity >= 0.9);
    }

    [Fact]
    public void Fuzzy_Disabled_GivesNotFound()
    {
        var result = MatchOne("The Hunchback of Notre Damme", new MatchOptions { Fuzzy = false });

        Assert.Equal(MatchStatus.NotFound, result.Status);
    }

    [Fact]
    public void Fuzzy_RespectsYear()
    {
        Assert.Equal(MatchStatus.PossibleFuzzy, MatchOne("The Hunchback of Notre Damme (1997)").Status);
        Assert.Equal(MatchStatus.NotFound, MatchOne("The Hunchback of Notre Damme (2010)").Status);
    }

    [Fact]
    public void Fuzzy_ShortTitles_DoNotMatchLoosely()
    {
        Assert.Equal(MatchStatus.NotFound, MatchOne("Cats").Status);
    }

    [Fact]
    public void NotFound_ForMissingTitles()
    {
        var result = MatchOne("Treasure Planet (2002)");

        Assert.Equal(MatchStatus.NotFound, result.Status);
        Assert.Empty(result.Candidates);
        Assert.False(result.IsSelectedByDefault);
    }

    [Fact]
    public void MovieAndSeries_SameName_Both_IsAmbiguous()
    {
        var result = MatchOne("Hercules");

        Assert.Equal(MatchStatus.Ambiguous, result.Status);
        Assert.Contains(result.Candidates, c => c.Entry.Kind == ItemKind.Movie);
        Assert.Contains(result.Candidates, c => c.Entry.Kind == ItemKind.Series);
    }

    [Fact]
    public void MovieAndSeries_SameName_MoviesOnly_Matches()
    {
        var result = MatchOne("Hercules", catalog: DisneyCatalog.MoviesOnly);

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get("Hercules", 1997).Id, result.SelectedId);
    }

    [Fact]
    public void Series_MatchByYear()
    {
        var result = MatchOne("DuckTales (2017)");

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get("DuckTales", 2017, ItemKind.Series).Id, result.SelectedId);
    }

    [Fact]
    public void SameNameAndYear_TwoItems_IsAmbiguous()
    {
        var a = new CatalogEntry(Guid.NewGuid(), "Twin", null, null, 2000, ItemKind.Movie, "Movies", null);
        var b = new CatalogEntry(Guid.NewGuid(), "Twin", null, null, 2000, ItemKind.Series, "Shows", null);

        var result = MatchOne("Twin (2000)", catalog: [a, b]);

        Assert.Equal(MatchStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void ItemWithoutYear_NoYearInList_Matches()
    {
        var result = MatchOne("The Black Cauldron");

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal(DisneyCatalog.Get("The Black Cauldron", null).Id, result.SelectedId);
    }

    [Fact]
    public void ItemWithoutYear_YearInList_NeedsConfirmation()
    {
        var result = MatchOne("The Black Cauldron (1985)");

        Assert.Equal(MatchStatus.Ambiguous, result.Status);
        Assert.False(result.IsSelectedByDefault);
        Assert.Equal(DisneyCatalog.Get("The Black Cauldron", null).Id, Assert.Single(result.Candidates).Entry.Id);
    }

    [Fact]
    public void AlreadyInCollection_IsGreyedOut()
    {
        var existing = DisneyCatalog.Get("Moana", 2016).Id;

        var result = MatchOne("Moana", new MatchOptions { ExistingMemberIds = new HashSet<Guid> { existing } });

        Assert.Equal(MatchStatus.AlreadyInCollection, result.Status);
        Assert.Equal(existing, result.SelectedId);
        Assert.False(result.IsSelectedByDefault);
    }

    [Fact]
    public void Ambiguous_FlagsCandidatesAlreadyInCollection()
    {
        var existing = DisneyCatalog.Get("Aladdin", 1992).Id;

        var result = MatchOne("Aladdin", new MatchOptions { ExistingMemberIds = new HashSet<Guid> { existing } });

        Assert.Equal(MatchStatus.Ambiguous, result.Status);
        Assert.True(result.Candidates.Single(c => c.Entry.Id == existing).InCollection);
    }

    [Fact]
    public void TwoLines_SameItem_AddedOnce()
    {
        var parsed = ListParser.Parse("WALL-E\nWALL·E (2008)\nMoana", 2026);

        var results = Matcher.Match(DisneyCatalog.All, parsed.Lines, new MatchOptions());

        Assert.Equal(3, results.Count);
        Assert.True(results[0].IsSelectedByDefault);
        Assert.False(results[1].IsSelectedByDefault);
        Assert.Equal(1, results[1].DuplicateOfLineNumber);
        Assert.Equal(results[0].SelectedId, results[1].SelectedId);
        Assert.True(results[2].IsSelectedByDefault);
    }

    [Fact]
    public void DuplicateCatalogIds_AreCollapsed()
    {
        var entry = DisneyCatalog.Get("Moana", 2016);

        var result = MatchOne("Moana", catalog: [entry, entry, entry with { Name = "Moana" }]);

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Single(result.Candidates);
    }

    [Fact]
    public void SortNameAndOriginalTitle_AreIndexed()
    {
        var entry = new CatalogEntry(Guid.NewGuid(), "Spirited Away", "Sen to Chihiro no Kamikakushi", "Spirited Away 01", 2001, ItemKind.Movie, "Anime", null);

        Assert.Equal(MatchStatus.Matched, MatchOne("Sen to Chihiro no Kamikakushi", catalog: [entry]).Status);
        Assert.Equal(MatchStatus.Matched, MatchOne("Spirited Away 01", catalog: [entry]).Status);
    }

    [Fact]
    public void EmptyCatalog_EverythingNotFound()
    {
        var parsed = ListParser.Parse("Moana\nCoco (2017)", 2026);

        var results = Matcher.Match([], parsed.Lines, new MatchOptions());

        Assert.All(results, r => Assert.Equal(MatchStatus.NotFound, r.Status));
    }

    [Fact]
    public void EmptyList_ReturnsNothing()
    {
        Assert.Empty(Matcher.Match(DisneyCatalog.All, [], new MatchOptions()));
    }

    [Fact]
    public void ResultsKeepLineOrder()
    {
        var parsed = ListParser.Parse("Moana\n# skip\nCoco\nNope Not Here\nUp", 2026);

        var results = Matcher.Match(DisneyCatalog.All, parsed.Lines, new MatchOptions());

        Assert.Equal(new[] { 1, 3, 4, 5 }, results.Select(r => r.Line.LineNumber));
    }

    [Fact]
    public void DiacriticsBulletsTabsAndCrLf_EndToEnd()
    {
        var text = "\uFEFF• Amélie\t2001\r\n  - Frozen II (2019)  \r\n1) Lion King, The, 1994\r\n";

        var results = Matcher.Match(DisneyCatalog.All, ListParser.Parse(text, 2026).Lines, new MatchOptions());

        Assert.All(results, r => Assert.Equal(MatchStatus.Matched, r.Status));
        Assert.Equal(DisneyCatalog.Get("The Lion King", 1994).Id, results[2].SelectedId);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void HugeList_AgainstLargeLibrary_IsFast()
    {
        var catalog = new List<CatalogEntry>(DisneyCatalog.All);
        var random = new Random(42);
        for (var i = 0; i < 20_000; i++)
        {
            catalog.Add(new CatalogEntry(Guid.NewGuid(), RandomTitle(random), null, null, 1950 + random.Next(75), ItemKind.Movie, "Movies", null));
        }

        var lines = Enumerable.Range(0, ListParser.MaxLines)
            .Select(i => i % 3 == 0 ? $"Missing Title Number {i}" : $"{catalog[random.Next(catalog.Count)].Name} {i}")
            .ToList();
        var parsed = ListParser.Parse(string.Join('\n', lines), 2026);

        var stopwatch = Stopwatch.StartNew();
        var results = Matcher.Match(catalog, parsed.Lines, new MatchOptions { Fuzzy = true, YearTolerance = 1 });
        stopwatch.Stop();

        Assert.Equal(parsed.Lines.Count, results.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Matching took {stopwatch.Elapsed}");
    }

    private static string RandomTitle(Random random)
    {
        string[] words = ["star", "night", "river", "dragon", "secret", "lost", "city", "dream", "shadow", "ocean", "king", "queen", "wild", "last", "first", "golden", "silent", "storm", "heart", "fire"];
        var count = 1 + random.Next(4);
        return string.Join(' ', Enumerable.Range(0, count).Select(_ => words[random.Next(words.Length)]));
    }
}
