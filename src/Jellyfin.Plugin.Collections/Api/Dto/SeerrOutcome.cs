namespace Jellyfin.Plugin.Collections.Api.Dto;

/// <summary>
/// What happened when a title was sent to Seerr.
/// </summary>
public enum SeerrOutcome
{
    /// <summary>
    /// A new request was created.
    /// </summary>
    Requested,

    /// <summary>
    /// Seerr already has a request for it.
    /// </summary>
    AlreadyRequested,

    /// <summary>
    /// Seerr reports it as available (fully or partly).
    /// </summary>
    AlreadyAvailable,

    /// <summary>
    /// Several Seerr results fit; the user must pick one.
    /// </summary>
    NeedsChoice,

    /// <summary>
    /// Seerr found nothing for the title.
    /// </summary>
    NotFound,

    /// <summary>
    /// Seerr rejected the request or could not be reached.
    /// </summary>
    Failed
}
