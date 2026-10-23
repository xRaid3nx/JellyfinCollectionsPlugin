# Decisions and deviations from the build prompt

Where the real Jellyfin 12.2.0 packages or this machine differed from the prompt, the real thing won. Each entry says what was decided and why.

## Environment

- **Plugin folder is `D:\Apps\Jellyfin\Data\plugins`**, not `C:\ProgramData\Jellyfin\Server\plugins`. This server runs `D:\Apps\Jellyfin\jellyfin.exe` with its data directory in `D:\Apps\Jellyfin\Data`. `deploy-dev.ps1` defaults to that path and accepts `-PluginsDir`.
- **The NuGet cache was already on D:** (`D:\Banaat\NuGet\packages`), so no change was needed.
- **`scripts/smoke-test.ps1` was added** (not in the prompt). It starts a throwaway Jellyfin 12.2 from the installed binaries, with its own data folder in `artifacts\sandbox`, port 8196, fake media files, and no internet metadata. It installs the plugin and exercises the real API end to end, including the admin-only checks. It never touches the real server.

## Build and packaging

- **`JellyfinVersion` MSBuild property** (default `12.2.0`) pins all three Jellyfin packages, so building for another server is `-p:JellyfinVersion=x.y.z` and CI can run a matrix.
- **`targetAbi` 12.2.0.0** acts as a minimum: Jellyfin refuses older servers and loads the plugin on newer ones, and then binary compatibility decides. That is why the version table in the README matters.
- **The manifest only lists the build for the newest Jellyfin version** in the matrix. Jellyfin picks a version by `targetAbi`, so one entry per plugin version is enough today. More can be added if older servers need to be supported.
- **Releases are cut locally with `scripts/publish-release.ps1`**, not by a tag-triggered workflow. The prompt's workflow committed `manifest.json` on GitHub, which would make GitHub newer than the local copy and break the "local is the source of truth" rule. The script updates `manifest.json` locally, then pushes it. `release.yaml` is kept as a manual fallback that only rebuilds zips for an existing tag.
- **CI is gated with `if: !github.event.repository.private`**. Adding a Jellyfin version is a one-line change to `JELLYFIN_VERSIONS`.
- **The repository is public** (made public at the owner's request on 2026-10-23), because Jellyfin installs plugins from a public `manifest.json` URL.

## Plugin surface

- **Page `Name` is `BatchCollections`** (used in the URL `configurationpage?name=BatchCollections`), and `DisplayName` is "Batch Collections" (shown in the sidebar). `EnableInMainMenu` places it in the dashboard sidebar.
- **Presets and last-used options use the standard plugin configuration API** (`ApiClient.getPluginConfiguration` / `updatePluginConfiguration`), not a custom endpoint, so they're stored and backed up like any other plugin setting.
- **Admin policy is `RequiresElevation`**, which is still the policy name in 12.2. It's verified by the smoke test (anonymous → 401, non-admin → 403).

## Jellyfin API behaviour

- **`AddToCollectionAsync` throws if any item id doesn't exist**, so Apply re-validates every id against the library first. Unknown ids are reported as failed instead of aborting the batch.
- **Collection members are read from `LinkedChildren`** (populated on BoxSet queries in 12.2), with `GetLinkedChildren()` as a fallback.
- **The catalog is read with `DtoOptions` stored-columns-only** in one query. Extras, owned items, and alternate versions are excluded. These instances are never saved.

## Matching

- **Library items with no year, when the list gives a year, become "Choose one"** (not Matched and not Different year), with a note. The year can't be confirmed, so the user decides.
- **Fuzzy matching uses normalised Levenshtein ≥ 0.90** on the loose key. For short titles that's effectively an exact match, which avoids false positives like *Up* vs *Us*. Fuzzy hits must also be within ±1 year when a year is given, and are never ticked by default.
- **Only `ForcedSortName`** is used as a sort-name alias. Jellyfin's computed sort name adds nothing over the normalised title.
- **Known limit: `101 Dalmatians` vs *One Hundred and One Dalmatians*.** Converting numerals to words in general would create more false matches than it fixes. It's documented in the README.
