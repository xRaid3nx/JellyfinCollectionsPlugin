using System.Linq;
using System.Text;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Tests;

public class ListParserTests
{
    private const int MaxYear = 2028;

    [Theory]
    [InlineData("The Lion King (1994)", "The Lion King", 1994)]
    [InlineData("The Lion King [1994]", "The Lion King", 1994)]
    [InlineData("The Lion King ( 1994 )", "The Lion King", 1994)]
    [InlineData("The Lion King - 1994", "The Lion King", 1994)]
    [InlineData("The Lion King – 1994", "The Lion King", 1994)]
    [InlineData("The Lion King, 1994", "The Lion King", 1994)]
    [InlineData("The Lion King,1994", "The Lion King", 1994)]
    [InlineData("The Lion King | 1994", "The Lion King", 1994)]
    [InlineData("The Lion King\t1994", "The Lion King", 1994)]
    [InlineData("Lion King, The, 1994", "Lion King, The", 1994)]
    [InlineData("\"Lion King, The\",1994", "Lion King, The", 1994)]
    [InlineData("- The Lion King (1994)", "The Lion King", 1994)]
    [InlineData("* The Lion King", "The Lion King", null)]
    [InlineData("• The Lion King", "The Lion King", null)]
    [InlineData("1. The Lion King", "The Lion King", null)]
    [InlineData("12) The Lion King", "The Lion King", null)]
    [InlineData("\"The Lion King\"", "The Lion King", null)]
    [InlineData("“The Lion King”", "The Lion King", null)]
    [InlineData("The Lion King,", "The Lion King", null)]
    [InlineData("The Lion King;  ", "The Lion King", null)]
    [InlineData("   The Lion King   ", "The Lion King", null)]
    [InlineData("Wreck-It Ralph", "Wreck-It Ralph", null)]
    [InlineData("Spider-Man: Into the Spider-Verse (2018)", "Spider-Man: Into the Spider-Verse", 2018)]
    public void ParseLine_ReadsTitleAndYear(string raw, string title, int? year)
    {
        var line = ListParser.ParseLine(1, raw, MaxYear);

        Assert.NotNull(line);
        Assert.Equal(title, line.Title);
        Assert.Equal(year, line.Year);
        Assert.Equal(raw, line.Raw);
    }

    [Theory]
    [InlineData("2012")]
    [InlineData("1917")]
    [InlineData("300")]
    [InlineData("Blade Runner 2049")]
    public void NumericTitles_StayTitles(string raw)
    {
        var line = ListParser.ParseLine(1, raw, MaxYear);

        Assert.NotNull(line);
        Assert.Equal(raw, line.Title);
        Assert.Null(line.Year);
        Assert.Null(line.BareYear);
    }

    [Fact]
    public void BareTrailingYear_IsKeptInTitle_WithFallback()
    {
        var line = ListParser.ParseLine(1, "Aladdin 1992", MaxYear);

        Assert.NotNull(line);
        Assert.Equal("Aladdin 1992", line.Title);
        Assert.Null(line.Year);
        Assert.Equal("Aladdin", line.BareYearTitle);
        Assert.Equal(1992, line.BareYear);
    }

    [Theory]
    [InlineData("Some Film (1899)")]
    [InlineData("Some Film (2099)")]
    [InlineData("Some Film, 3000")]
    public void OutOfRangeYear_IsNotAYear(string raw)
    {
        var line = ListParser.ParseLine(1, raw, MaxYear);

        Assert.NotNull(line);
        Assert.Null(line.Year);
        Assert.Equal(raw, line.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("# Disney classics")]
    [InlineData("   # indented comment")]
    [InlineData("// another comment")]
    [InlineData("-  ")]
    public void BlankAndCommentLines_AreIgnored(string raw)
    {
        Assert.Null(ListParser.ParseLine(1, raw, MaxYear));
    }

    [Fact]
    public void Parse_HandlesCrLfLfBomAndLineNumbers()
    {
        var text = "\uFEFFThe Lion King (1994)\r\n# comment\r\n\nAladdin\rMulan (1998)  \n";

        var result = ListParser.Parse(text, 2026);

        Assert.Equal(3, result.Lines.Count);
        Assert.Equal(new[] { 1, 4, 5 }, result.Lines.Select(l => l.LineNumber));
        Assert.Equal("The Lion King", result.Lines[0].Title);
        Assert.Equal("Aladdin", result.Lines[1].Title);
        Assert.Equal(1998, result.Lines[2].Year);
        Assert.Equal(3, result.IgnoredCount);
    }

    [Fact]
    public void Parse_CollapsesDuplicates()
    {
        var text = "The Lion King (1994)\nLion King, The (1994)\n- the lion king [1994]\nThe Lion King (2019)\nThe Lion King";

        var result = ListParser.Parse(text, 2026);

        Assert.Equal(3, result.Lines.Count);
        Assert.Equal(2, result.Duplicates.Count);
        Assert.All(result.Duplicates, d => Assert.Equal(1, d.DuplicateOfLineNumber));
    }

    [Fact]
    public void Parse_EmptyText_ReturnsNothing()
    {
        Assert.Empty(ListParser.Parse(string.Empty, 2026).Lines);
        Assert.Empty(ListParser.Parse(null, 2026).Lines);
        Assert.Empty(ListParser.Parse("\n\n# only comments\n", 2026).Lines);
    }

    [Fact]
    public void Parse_DefaultOverload_UsesCurrentYear()
    {
        Assert.Single(ListParser.Parse("Moana (2016)").Lines);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n  ")]
    public void Validate_RejectsEmpty(string? text)
    {
        Assert.NotNull(ListParser.Validate(text));
    }

    [Fact]
    public void Validate_AcceptsMaxLines_RejectsMore()
    {
        var ok = string.Join('\n', Enumerable.Repeat("Moana", ListParser.MaxLines));
        var tooMany = ok + "\nMoana";

        Assert.Null(ListParser.Validate(ok));
        Assert.Contains("5,000", ListParser.Validate(tooMany), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsHugeText()
    {
        var builder = new StringBuilder();
        builder.Append('x', ListParser.MaxCharacters + 1);

        Assert.Contains("too large", ListParser.Validate(builder.ToString()), System.StringComparison.Ordinal);
    }
}
