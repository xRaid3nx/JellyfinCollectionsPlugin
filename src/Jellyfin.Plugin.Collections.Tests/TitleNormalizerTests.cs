using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Tests;

public class TitleNormalizerTests
{
    [Theory]
    [InlineData("The Lion King", "lion king")]
    [InlineData("Lion King, The", "lion king")]
    [InlineData("  The   Lion  King  ", "lion king")]
    [InlineData("Amélie", "amelie")]
    [InlineData("WALL·E", "wall e")]
    [InlineData("WALL-E", "wall e")]
    [InlineData("Wreck-It Ralph", "wreck it ralph")]
    [InlineData("Tron: Legacy", "tron legacy")]
    [InlineData("Lilo & Stitch", "lilo and stitch")]
    [InlineData("Ocean’s Eleven", "oceans eleven")]
    [InlineData("Aladdin (1992)", "aladdin")]
    [InlineData("Aladdin [1992]", "aladdin")]
    [InlineData("A Bug's Life", "bugs life")]
    [InlineData("An American Tail", "american tail")]
    [InlineData("The", "the")]
    [InlineData("Æon Flux", "aeon flux")]
    [InlineData("Pokémon: Detective Pikachu", "pokemon detective pikachu")]
    [InlineData("1917", "1917")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Strict_ProducesExpectedKey(string? input, string expected)
    {
        Assert.Equal(expected, TitleNormalizer.Strict(input));
    }

    [Theory]
    [InlineData("Frozen II", "Frozen 2")]
    [InlineData("Frozen Two", "Frozen 2")]
    [InlineData("Toy Story III", "Toy Story 3")]
    [InlineData("Big Hero Six", "Big Hero 6")]
    [InlineData("Rocky IV: Something", "Rocky 4: Something")]
    [InlineData("Disney's A Christmas Carol", "A Christmas Carol")]
    [InlineData("Walt Disney's Fantasia", "Fantasia")]
    [InlineData("Disney Pixar's Up", "Up")]
    [InlineData("Marvel's The Avengers", "The Avengers")]
    [InlineData("Pixar's Cars", "Cars")]
    [InlineData("Disney Moana", "Moana")]
    [InlineData("WALL·E", "WALL-E")]
    [InlineData("Wreck-It Ralph", "Wreck It Ralph")]
    [InlineData("Tron: Legacy", "Tron Legacy")]
    [InlineData("Pirates of the Caribbean: The Curse of the Black Pearl", "Pirates of the Caribbean The Curse of the Black Pearl")]
    [InlineData("Spider-Man: Into the Spider-Verse", "Spider Man Into the Spider Verse")]
    [InlineData("Lion King, The", "The Lion King")]
    public void Loose_TreatsVariantsAsEqual(string a, string b)
    {
        Assert.Equal(TitleNormalizer.Loose(a), TitleNormalizer.Loose(b));
    }

    [Theory]
    [InlineData("I, Robot", "i robot")]
    [InlineData("Iron Man", "iron man")]
    [InlineData("Disney", "disney")]
    [InlineData("Disney's", "disneys")]
    [InlineData("Up", "up")]
    public void Loose_LeavesProtectedWordsAlone(string input, string expected)
    {
        Assert.Equal(expected, TitleNormalizer.Loose(input));
    }

    [Theory]
    [InlineData("Frozen II", "Frozen 2")]
    [InlineData("Disney's A Christmas Carol", "A Christmas Carol")]
    public void Strict_DoesNotApplyLooseRules(string a, string b)
    {
        Assert.NotEqual(TitleNormalizer.Strict(a), TitleNormalizer.Strict(b));
    }

    [Fact]
    public void NumbersAsWords_BeyondTen_AreAKnownLimit()
    {
        Assert.NotEqual(
            TitleNormalizer.Loose("101 Dalmatians"),
            TitleNormalizer.Loose("One Hundred and One Dalmatians"));
    }

    [Fact]
    public void NumberWord_InMiddleOfTitle_IsNotFolded()
    {
        Assert.Equal("two towers", TitleNormalizer.Loose("The Two Towers"));
    }
}
