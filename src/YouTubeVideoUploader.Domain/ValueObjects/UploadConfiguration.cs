using System;
using System.Collections.Generic;
using YouTubeVideoUploader.Domain.Enums;

namespace YouTubeVideoUploader.Domain.ValueObjects;

/// <summary>
/// Represents the configuration options for uploading a batch of videos.
/// </summary>
public record UploadConfiguration
{
    /// <summary>
    /// Gets the game name (e.g. "V Rising", "Doom 2").
    /// </summary>
    public string GameName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the target YouTube playlist ID.
    /// </summary>
    public string PlaylistId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of static tags added to all uploaded videos.
    /// </summary>
    public IReadOnlyList<string> StaticTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the template for the video description.
    /// Can contain placeholders like {GameName}.
    /// </summary>
    public string DescriptionTemplate { get; init; } = "Gameplay from {GameName}";

    /// <summary>
    /// Gets the YouTube video category ID (e.g., "20" for Gaming).
    /// </summary>
    public string CategoryId { get; init; } = "20";

    /// <summary>
    /// Gets the default privacy status for uploaded videos.
    /// Note: Scheduled publishing requires the privacy status to be Private.
    /// </summary>
    public PrivacyStatus PrivacyStatus { get; init; } = PrivacyStatus.Private;

    /// <summary>
    /// Gets the scheduled publishing schedule.
    /// </summary>
    public PublishSchedule Schedule { get; init; } = new();
}
