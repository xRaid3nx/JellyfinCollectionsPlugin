# GitHub — JellyfinCollectionsPlugin

Repository: https://github.com/xRaid3nx/JellyfinCollectionsPlugin (**public**, so Jellyfin servers can install from its `manifest.json`).

Local `D:\Banaat\JellyfinCollectionsPlugin` is still the source of truth. GitHub is the published copy and backup. Don't edit files on GitHub's website; change them locally and push.

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
https://raw.githubusercontent.com/xRaid3nx/JellyfinCollectionsPlugin/main/manifest.json
```
