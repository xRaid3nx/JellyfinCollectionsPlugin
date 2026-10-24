using Jellyfin.Plugin.Collections.Core;

namespace Jellyfin.Plugin.Collections.Services;

/// <summary>
/// What happened when artwork was added to a new collection.
/// </summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Message">A readable explanation.</param>
public sealed record PosterResult(PosterOutcome Outcome, string Message);
