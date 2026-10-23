using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// Turns titles into comparison keys. The same functions are applied to list titles and library titles.
/// </summary>
/// <remarks>
/// <para>Strict key: diacritics removed, lowercase, punctuation folded, trailing/leading articles ignored, trailing year tag removed.</para>
/// <para>Loose key: strict, plus studio prefixes removed and number words / roman numerals (2-10) folded to digits
/// when they end the title or come right before a colon.</para>
/// </remarks>
public static partial class TitleNormalizer
{
    private static readonly string[] _articles = ["the", "a", "an"];

    // Longest first so "walt disneys" wins over "disneys".
    private static readonly string[][] _studioPrefixes =
    [
        ["walt", "disneys"],
        ["disney", "pixars"],
        ["disneys"],
        ["pixars"],
        ["marvels"],
        ["disney"]
    ];

    private static readonly Dictionary<string, string> _numberWords = new(StringComparer.Ordinal)
    {
        ["2"] = "2", ["ii"] = "2", ["two"] = "2",
        ["3"] = "3", ["iii"] = "3", ["three"] = "3",
        ["4"] = "4", ["iv"] = "4", ["four"] = "4",
        ["5"] = "5", ["v"] = "5", ["five"] = "5",
        ["6"] = "6", ["vi"] = "6", ["six"] = "6",
        ["7"] = "7", ["vii"] = "7", ["seven"] = "7",
        ["8"] = "8", ["viii"] = "8", ["eight"] = "8",
        ["9"] = "9", ["ix"] = "9", ["nine"] = "9",
        ["10"] = "10", ["x"] = "10", ["ten"] = "10"
    };

    private static readonly Dictionary<char, string> _letterFolds = new()
    {
        ['æ'] = "ae", ['Æ'] = "ae", ['œ'] = "oe", ['Œ'] = "oe",
        ['ø'] = "o", ['Ø'] = "o", ['ß'] = "ss", ['ł'] = "l", ['Ł'] = "l",
        ['đ'] = "d", ['Đ'] = "d", ['þ'] = "th", ['Þ'] = "th"
    };

    /// <summary>
    /// Gets the strict comparison key.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns>The key, or an empty string for a blank title.</returns>
    public static string Strict(string? title) => Normalize(title, loose: false);

    /// <summary>
    /// Gets the loose comparison key.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns>The key, or an empty string for a blank title.</returns>
    public static string Loose(string? title) => Normalize(title, loose: true);

    private static string Normalize(string? title, bool loose)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var text = title.Trim();
        var withoutYear = TrailingYearTagRegex().Replace(text, string.Empty).Trim();
        if (withoutYear.Length > 0)
        {
            text = withoutYear;
        }

        var article = TrailingArticleRegex().Match(text);
        if (article.Success)
        {
            text = article.Groups["art"].Value + " " + article.Groups["rest"].Value;
        }

        var tokens = new List<string>();
        foreach (var segment in SegmentSeparatorRegex().Split(text))
        {
            var segmentTokens = Tokenize(segment);
            if (loose && segmentTokens.Count > 0
                && _numberWords.TryGetValue(segmentTokens[^1], out var digit))
            {
                segmentTokens[^1] = digit;
            }

            tokens.AddRange(segmentTokens);
        }

        if (loose)
        {
            RemoveStudioPrefix(tokens);
        }

        if (tokens.Count > 1 && Array.IndexOf(_articles, tokens[0]) >= 0)
        {
            tokens.RemoveAt(0);
        }

        return string.Join(' ', tokens);
    }

    private static List<string> Tokenize(string segment)
    {
        var decomposed = segment.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (c is '\'' or '’' or '‘' or '`' or '´' or 'ʼ')
            {
                continue;
            }

            if (c == '&')
            {
                builder.Append(" and ");
            }
            else if (_letterFolds.TryGetValue(c, out var folded))
            {
                builder.Append(folded);
            }
            else if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(' ');
            }
        }

        return [.. builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static void RemoveStudioPrefix(List<string> tokens)
    {
        foreach (var prefix in _studioPrefixes)
        {
            if (tokens.Count <= prefix.Length)
            {
                continue;
            }

            var matches = true;
            for (var i = 0; i < prefix.Length; i++)
            {
                if (!string.Equals(tokens[i], prefix[i], StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                tokens.RemoveRange(0, prefix.Length);
                return;
            }
        }
    }

    [GeneratedRegex(@"\s*[\(\[]\s*(18|19|20|21)\d{2}\s*[\)\]]\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingYearTagRegex();

    [GeneratedRegex(@"^(?<rest>.+?),\s*(?<art>the|a|an)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex TrailingArticleRegex();

    [GeneratedRegex(@"\s*:\s*|\s+[-–—]\s+", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentSeparatorRegex();
}
