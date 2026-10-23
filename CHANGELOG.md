# Changelog

All notable changes to this project are documented here. Versions use Jellyfin's four-part format.

## 1.0.0.0

First release, for Jellyfin 12.2.

- Dashboard page: paste a list, preview matches, and apply the selected items to a new or existing collection.
- Statuses: Matched, Already in collection, Choose one, Year off by 1, Close spelling, Different year, Not found.
- List parser that handles years in several formats, bullets, numbering, quotes, comments, BOM, and CRLF/LF, and reports duplicate lines.
- Title normalisation: accents, punctuation, articles, "Title, The", `&`/and, roman numerals and number words, and studio prefixes.
- Fuzzy suggestions (90 %+ similar) that are never selected automatically.
- Presets and remembered options, stored in the plugin configuration.
- Report with "Copy Not found" and "Copy full report".
- Admin-only REST API under `/BatchCollections`.
