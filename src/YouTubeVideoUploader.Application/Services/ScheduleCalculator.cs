using System;
using System.Collections.Generic;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Services;

/// <summary>
/// Service responsible for calculating dates for scheduled publishing.
/// </summary>
public class ScheduleCalculator
{
    /// <summary>
    /// Calculates the scheduled publish dates for a list of videos.
    /// </summary>
    /// <param name="videoCount">The total number of videos to schedule.</param>
    /// <param name="schedule">The scheduling policy parameters.</param>
    /// <returns>A read-only list of computed UTC DateTimes.</returns>
    public IReadOnlyList<DateTime> CalculatePublishDates(int videoCount, PublishSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var dates = new List<DateTime>(videoCount);
        for (int i = 0; i < videoCount; i++)
        {
            dates.Add(schedule.CalculatePublishDateTime(i));
        }

        return dates;
    }
}
