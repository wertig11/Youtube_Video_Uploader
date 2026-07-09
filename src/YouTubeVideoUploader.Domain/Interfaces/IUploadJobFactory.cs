using System;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Factory for creating UploadJob instances from video files and configuration.
/// </summary>
public interface IUploadJobFactory
{
    /// <summary>
    /// Creates an UploadJob.
    /// </summary>
    /// <param name="file">The video file.</param>
    /// <param name="config">The upload configuration.</param>
    /// <param name="publishDate">The computed publish date (null if not scheduled).</param>
    /// <returns>A new UploadJob entity.</returns>
    UploadJob CreateJob(VideoFile file, UploadConfiguration config, DateTime? publishDate);
}
