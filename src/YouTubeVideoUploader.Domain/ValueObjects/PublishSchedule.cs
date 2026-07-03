using System;

namespace YouTubeVideoUploader.Domain.ValueObjects;

/// <summary>
/// Represents the publish schedule configuration for a batch of videos.
/// </summary>
public record PublishSchedule
{
    /// <summary>
    /// Gets the start date from which videos are scheduled.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the time of day at which videos should be published (usually UTC).
    /// </summary>
    public TimeOnly PublishTime { get; init; }

    /// <summary>
    /// Gets the interval in days between publications.
    /// 1 means daily, 2 means every second day, etc.
    /// </summary>
    public int IntervalDays { get; init; } = 1;

    /// <summary>
    /// Calculates the scheduled publish date and time for a video at the specified index.
    /// </summary>
    /// <param name="videoIndex">The zero-based index of the video in the batch.</param>
    /// <returns>The calculated DateTime in UTC.</returns>
    public DateTime CalculatePublishDateTime(int videoIndex)
    {
        int stepDays = IntervalDays + 1;
        DateOnly date = StartDate.AddDays(videoIndex * stepDays);
        return date.ToDateTime(PublishTime, DateTimeKind.Utc);
    }
}
