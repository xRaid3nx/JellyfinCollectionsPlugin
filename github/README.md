# GitHub — PasteCollections

Repository: https://github.com/xRaid3nx/PasteCollections (**public**, so Jellyfin servers can install from its `manifest.json`).

**Paste Collections** is the Jellyfin plugin: paste a list of movie/show titles, preview matches, and add them to a collection. Optional **Seerr** (or Jellyseerr/Overseerr) support requests titles that aren’t in your library from the preview or report.

Local `D:\Banaat\JellyfinCollectionsPlugin` is still the source of truth (folder name can stay). GitHub is the published copy and backup. Don't edit files on GitHub's website; change them locally and push.

Old URL `https://github.com/xRaid3nx/JellyfinCollectionsPlugin` redirects here.

## Commands

From repo root:

```powershell
.\github\push.ps1                                  # push commits and tags (backup)
.\scripts\publish-release.ps1 -Version 1.0.1.0     # cut a release (see docs/DEVELOPMENT.md)
.\github\pull.ps1                                  # restore from GitHub, only when needed
```

Shared implementation: `D:\Banaat\General\Scripts\Push-RepoBackup.ps1`

## Plugin repository URL (for Jellyfin)

```text
https://raw.githubusercontent.com/xRaid3nx/PasteCollections/main/manifest.json
```
