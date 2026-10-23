using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Collections.Core;

/// <summary>
/// Finds library titles whose loose key is within a normalised Levenshtein similarity of a query.
/// </summary>
internal sealed class FuzzyIndex
{
    private const int HistogramSize = 37;

    private readonly Dictionary<int, List<Key>> _byLength = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzyIndex"/> class.
    /// </summary>
    /// <param name="keys">Loose keys mapped to the entries they came from.</param>
    public FuzzyIndex(IReadOnlyDictionary<string, List<CatalogEntry>> keys)
    {
        foreach (var (text, entries) in keys)
        {
            if (text.Length == 0)
            {
                continue;
            }

            if (!_byLength.TryGetValue(text.Length, out var bucket))
            {
                bucket = [];
                _byLength[text.Length] = bucket;
            }

            bucket.Add(new Key(text, Histogram(text), entries));
        }
    }

    /// <summary>
    /// Finds entries whose key similarity is at least <paramref name="threshold"/>.
    /// </summary>
    /// <param name="query">The loose key of the list title.</param>
    /// <param name="threshold">Minimum similarity (0-1).</param>
    /// <returns>Matching entries with their similarity, best first.</returns>
    public List<(CatalogEntry Entry, double Similarity)> Find(string query, double threshold)
    {
        var results = new List<(CatalogEntry Entry, double Similarity)>();
        if (query.Length == 0)
        {
            return results;
        }

        var queryHistogram = Histogram(query);
        var minLength = (int)Math.Ceiling(query.Length * threshold);
        var maxLength = (int)Math.Floor(query.Length / threshold);
        for (var length = minLength; length <= maxLength; length++)
        {
            if (!_byLength.TryGetValue(length, out var bucket))
            {
                continue;
            }

            var maxDistance = (int)Math.Floor(((1 - threshold) * Math.Max(length, query.Length)) + 1e-9);
            foreach (var key in bucket)
            {
                if (string.Equals(key.Text, query, StringComparison.Ordinal))
                {
                    continue;
                }

                if (HistogramDistance(queryHistogram, key.Histogram) > 2 * maxDistance)
                {
                    continue;
                }

                var distance = BoundedLevenshtein(query, key.Text, maxDistance);
                if (distance > maxDistance)
                {
                    continue;
                }

                var similarity = 1.0 - ((double)distance / Math.Max(length, query.Length));
                if (similarity + 1e-9 < threshold)
                {
                    continue;
                }

                foreach (var entry in key.Entries)
                {
                    results.Add((entry, similarity));
                }
            }
        }

        results.Sort((a, b) => b.Similarity.CompareTo(a.Similarity));
        return results;
    }

    /// <summary>
    /// Computes the Levenshtein distance, giving up once it exceeds <paramref name="max"/>.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <param name="max">The largest distance of interest.</param>
    /// <returns>The distance, or <c>max + 1</c> when larger than <paramref name="max"/>.</returns>
    internal static int BoundedLevenshtein(string a, string b, int max)
    {
        if (Math.Abs(a.Length - b.Length) > max)
        {
            return max + 1;
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            var rowMin = current[0];
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                rowMin = Math.Min(rowMin, current[j]);
            }

            if (rowMin > max)
            {
                return max + 1;
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    private static int[] Histogram(string text)
    {
        var histogram = new int[HistogramSize];
        foreach (var c in text)
        {
            if (c is >= 'a' and <= 'z')
            {
                histogram[c - 'a']++;
            }
            else if (c is >= '0' and <= '9')
            {
                histogram[26 + (c - '0')]++;
            }
            else
            {
                histogram[36]++;
            }
        }

        return histogram;
    }

    private static int HistogramDistance(int[] a, int[] b)
    {
        var total = 0;
        for (var i = 0; i < HistogramSize; i++)
        {
            total += Math.Abs(a[i] - b[i]);
        }

        return total;
    }

    private sealed record Key(string Text, int[] Histogram, List<CatalogEntry> Entries);
}
