namespace YouTubeVideoUploader.Domain.Enums;

/// <summary>
/// Represents the privacy status of a YouTube video.
/// </summary>
public enum PrivacyStatus
{
    /// <summary>
    /// Video is publicly visible and listed on YouTube; accessible to everyone.
    /// </summary>
    Public,

    /// <summary>
    /// Video is not shown in public searches or on the channel but can be accessed by anyone with the direct link.
    /// </summary>
    Unlisted,

    /// <summary>
    /// Video is only visible to the owner and selected users; requires explicit permission to view.
    /// </summary>
    Private
}
