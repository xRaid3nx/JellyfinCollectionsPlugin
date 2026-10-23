using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// Parses a pasted list of titles, one per line, with an optional release year.
/// </summary>
public static partial class ListParser
{
    /// <summary>
    /// The maximum number of lines accepted.
    /// </summary>
    public const int MaxLines = 5000;

    /// <summary>
    /// The maximum list size in characters (about 1 MB).
    /// </summary>
    public const int MaxCharacters = 1_000_000;

    /// <summary>
    /// The earliest year recognised as a release year.
    /// </summary>
    public const int MinYear = 1900;

    /// <summary>
    /// Checks the list against the size limits.
    /// </summary>
    /// <param name="text">The pasted text.</param>
    /// <returns>A readable error, or null when the list is acceptable.</returns>
    public static string? Validate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "The list is empty. Paste one title per line.";
        }

        if (text.Length > MaxCharacters)
        {
            return string.Create(CultureInfo.InvariantCulture, $"The list is too large ({text.Length:N0} characters). The limit is {MaxCharacters:N0}.");
        }

        var lines = CountLines(text);
        if (lines > MaxLines)
        {
            return string.Create(CultureInfo.InvariantCulture, $"The list has {lines:N0} lines. The limit is {MaxLines:N0}; split it into smaller lists.");
        }

        return null;
    }

    /// <summary>
    /// Parses the list using the current year to bound valid years.
    /// </summary>
    /// <param name="text">The pasted text.</param>
    /// <returns>The parse result.</returns>
    public static ParseResult Parse(string? text) => Parse(text, DateTime.UtcNow.Year);

    /// <summary>
    /// Parses the list.
    /// </summary>
    /// <param name="text">The pasted text.</param>
    /// <param name="currentYear">The current year; years up to <c>currentYear + 2</c> are valid.</param>
    /// <returns>The parse result.</returns>
    public static ParseResult Parse(string? text, int currentYear)
    {
        var lines = new List<ParsedLine>();
        var duplicates = new List<DuplicateLine>();
        var ignored = 0;
        if (string.IsNullOrEmpty(text))
        {
            return new ParseResult(lines, duplicates, ignored);
        }

        var maxYear = currentYear + 2;
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var rawLines = LineBreakRegex().Split(text.TrimStart('\uFEFF'));
        for (var i = 0; i < rawLines.Length; i++)
        {
            var raw = rawLines[i].Replace("\uFEFF", string.Empty, StringComparison.Ordinal);
            var parsed = ParseLine(i + 1, raw, maxYear);
            if (parsed is null)
            {
                ignored++;
                continue;
            }

            var key = TitleNormalizer.Strict(parsed.Title) + "|" + parsed.Year?.ToString(CultureInfo.InvariantCulture);
            if (seen.TryGetValue(key, out var firstLine))
            {
                duplicates.Add(new DuplicateLine(parsed, firstLine));
                continue;
            }

            seen[key] = parsed.LineNumber;
            lines.Add(parsed);
        }

        return new ParseResult(lines, duplicates, ignored);
    }

    /// <summary>
    /// Parses a single line.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number.</param>
    /// <param name="raw">The raw line.</param>
    /// <param name="maxYear">The latest valid year.</param>
    /// <returns>The parsed line, or null when the line is blank or a comment.</returns>
    internal static ParsedLine? ParseLine(int lineNumber, string raw, int maxYear)
    {
        var text = raw.Trim();
        if (text.Length == 0 || text.StartsWith('#') || text.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        text = BulletRegex().Replace(text, string.Empty);
        text = TrimTrailingSeparators(text);
        text = StripQuotes(text);
        if (!text.Any(char.IsLetterOrDigit))
        {
            return null;
        }

        var yearMatch = BracketYearRegex().Match(text);
        if (!yearMatch.Success)
        {
            yearMatch = SeparatorYearRegex().Match(text);
        }

        if (yearMatch.Success && TryYear(yearMatch.Groups["y"].Value, maxYear, out var year))
        {
            var title = StripQuotes(TrimTrailingSeparators(yearMatch.Groups["t"].Value));
            if (title.Length > 0)
            {
                return new ParsedLine(lineNumber, raw, title, year);
            }
        }

        var bare = BareYearRegex().Match(text);
        if (bare.Success && TryYear(bare.Groups["y"].Value, maxYear, out var bareYear))
        {
            return new ParsedLine(lineNumber, raw, text, null, bare.Groups["t"].Value.Trim(), bareYear);
        }

        return new ParsedLine(lineNumber, raw, text, null);
    }

    private static bool TryYear(string value, int maxYear, out int year)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out year)
            && year >= MinYear
            && year <= maxYear;
    }

    private static string TrimTrailingSeparators(string text) => text.Trim().TrimEnd(',', ';').Trim();

    private static string StripQuotes(string text)
    {
        while (text.Length >= 2 && IsQuotePair(text[0], text[^1]))
        {
            text = text[1..^1].Trim();
        }

        return text;
    }

    private static bool IsQuotePair(char open, char close)
    {
        return (open == '"' && close == '"')
            || (open == '\'' && close == '\'')
            || (open == '“' && close == '”')
            || (open == '‘' && close == '’')
            || (open == '«' && close == '»');
    }

    private static int CountLines(string text)
    {
        var count = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
            {
                count++;
            }
        }

        return count;
    }

    [GeneratedRegex(@"\r\n|\n|\r", RegexOptions.CultureInvariant)]
    private static partial Regex LineBreakRegex();

    // Bullets (-, *, •, ·, ▪, ◦, ‣, –, —) need a following space; numbering is "1." or "1)".
    [GeneratedRegex(@"^(?:[-*•·▪◦‣–—]+\s+|\d{1,4}[.)]\s+|\d{1,4}\)\s*)", RegexOptions.CultureInvariant)]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"^(?<t>.+?)\s*[\(\[]\s*(?<y>\d{4})\s*[\)\]]$", RegexOptions.CultureInvariant)]
    private static partial Regex BracketYearRegex();

    [GeneratedRegex(@"^(?<t>.+?)(?:\s+[-–—]\s+|\s*,\s*|\s*\|\s*|\t+\s*)(?<y>\d{4})$", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorYearRegex();

    [GeneratedRegex(@"^(?<t>.*\S)\s+(?<y>\d{4})$", RegexOptions.CultureInvariant)]
    private static partial Regex BareYearRegex();
}
