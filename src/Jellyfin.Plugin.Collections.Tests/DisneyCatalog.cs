using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Tests;

/// <summary>
/// A synthetic library of ~60 Disney titles, including the remake pairs.
/// </summary>
internal static class DisneyCatalog
{
    private static readonly (string Name, int? Year, ItemKind Kind, string? Original)[] _titles =
    [
        ("Snow White and the Seven Dwarfs", 1937, ItemKind.Movie, null),
        ("Pinocchio", 1940, ItemKind.Movie, null),
        ("Fantasia", 1940, ItemKind.Movie, null),
        ("Dumbo", 1941, ItemKind.Movie, null),
        ("Dumbo", 2019, ItemKind.Movie, null),
        ("Bambi", 1942, ItemKind.Movie, null),
        ("Cinderella", 1950, ItemKind.Movie, null),
        ("Cinderella", 2015, ItemKind.Movie, null),
        ("Alice in Wonderland", 1951, ItemKind.Movie, null),
        ("Peter Pan", 1953, ItemKind.Movie, null),
        ("Lady and the Tramp", 1955, ItemKind.Movie, null),
        ("Sleeping Beauty", 1959, ItemKind.Movie, null),
        ("One Hundred and One Dalmatians", 1961, ItemKind.Movie, null),
        ("The Sword in the Stone", 1963, ItemKind.Movie, null),
        ("The Jungle Book", 1967, ItemKind.Movie, null),
        ("The Jungle Book", 2016, ItemKind.Movie, null),
        ("The Aristocats", 1970, ItemKind.Movie, null),
        ("Robin Hood", 1973, ItemKind.Movie, null),
        ("The Little Mermaid", 1989, ItemKind.Movie, null),
        ("Beauty and the Beast", 1991, ItemKind.Movie, null),
        ("Beauty and the Beast", 2017, ItemKind.Movie, null),
        ("Aladdin", 1992, ItemKind.Movie, null),
        ("Aladdin", 2019, ItemKind.Movie, null),
        ("The Lion King", 1994, ItemKind.Movie, null),
        ("The Lion King", 2019, ItemKind.Movie, null),
        ("Pocahontas", 1995, ItemKind.Movie, null),
        ("Toy Story", 1995, ItemKind.Movie, null),
        ("Toy Story 2", 1999, ItemKind.Movie, null),
        ("Toy Story 3", 2010, ItemKind.Movie, null),
        ("The Hunchback of Notre Dame", 1996, ItemKind.Movie, null),
        ("Hercules", 1997, ItemKind.Movie, null),
        ("Mulan", 1998, ItemKind.Movie, null),
        ("Mulan", 2020, ItemKind.Movie, null),
        ("A Bug's Life", 1998, ItemKind.Movie, null),
        ("Tarzan", 1999, ItemKind.Movie, null),
        ("Monsters, Inc.", 2001, ItemKind.Movie, null),
        ("Lilo & Stitch", 2002, ItemKind.Movie, null),
        ("Lilo & Stitch", 2025, ItemKind.Movie, null),
        ("Pirates of the Caribbean: The Curse of the Black Pearl", 2003, ItemKind.Movie, null),
        ("Finding Nemo", 2003, ItemKind.Movie, null),
        ("The Incredibles", 2004, ItemKind.Movie, null),
        ("Cars", 2006, ItemKind.Movie, null),
        ("Ratatouille", 2007, ItemKind.Movie, null),
        ("WALL-E", 2008, ItemKind.Movie, null),
        ("Up", 2009, ItemKind.Movie, null),
        ("A Christmas Carol", 2009, ItemKind.Movie, null),
        ("The Princess and the Frog", 2009, ItemKind.Movie, null),
        ("Tron: Legacy", 2010, ItemKind.Movie, null),
        ("Tangled", 2010, ItemKind.Movie, null),
        ("Wreck-It Ralph", 2012, ItemKind.Movie, null),
        ("Brave", 2012, ItemKind.Movie, null),
        ("Frozen", 2013, ItemKind.Movie, null),
        ("Frozen II", 2019, ItemKind.Movie, null),
        ("Big Hero 6", 2014, ItemKind.Movie, null),
        ("Zootopia", 2016, ItemKind.Movie, null),
        ("Moana", 2016, ItemKind.Movie, null),
        ("Coco", 2017, ItemKind.Movie, null),
        ("Spider-Man: Into the Spider-Verse", 2018, ItemKind.Movie, null),
        ("Encanto", 2021, ItemKind.Movie, null),
        ("Amélie", 2001, ItemKind.Movie, "Le Fabuleux Destin d'Amélie Poulain"),
        ("Fantasia 2000", 1999, ItemKind.Movie, null),
        ("2012", 2009, ItemKind.Movie, null),
        ("1917", 2019, ItemKind.Movie, null),
        ("300", 2007, ItemKind.Movie, null),
        ("Blade Runner 2049", 2017, ItemKind.Movie, null),
        ("Gargoyles", 1994, ItemKind.Series, null),
        ("DuckTales", 1987, ItemKind.Series, null),
        ("DuckTales", 2017, ItemKind.Series, null),
        ("Hercules", 1998, ItemKind.Series, null),
        ("Fantasia 2000 Untitled Remaster", null, ItemKind.Movie, null),
        ("The Black Cauldron", null, ItemKind.Movie, null)
    ];

    public static IReadOnlyList<CatalogEntry> All { get; } = Build();

    public static IReadOnlyList<CatalogEntry> MoviesOnly { get; } = All.Where(e => e.Kind == ItemKind.Movie).ToList();

    public static CatalogEntry Get(string name, int? year, ItemKind kind = ItemKind.Movie)
    {
        return All.Single(e => e.Name == name && e.Year == year && e.Kind == kind);
    }

    private static List<CatalogEntry> Build()
    {
        var list = new List<CatalogEntry>();
        for (var i = 0; i < _titles.Length; i++)
        {
            var (name, year, kind, original) = _titles[i];
            var id = new Guid(i + 1, 0, 0, new byte[8]);
            var library = kind == ItemKind.Movie ? "Movies" : "Shows";
            list.Add(new CatalogEntry(id, name, original, null, year, kind, library, $"D:\\Media\\{library}\\{name}"));
        }

        return list;
    }
}
