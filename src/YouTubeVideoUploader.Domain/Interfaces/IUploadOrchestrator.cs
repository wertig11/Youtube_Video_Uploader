using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Represents the progress of a batch upload operation.
/// </summary>
public record BatchProgress(
    int CompletedCount, 
    int TotalCount, 
    string CurrentVideoTitle, 
    long CurrentVideoBytesSent, 
    long CurrentVideoTotalBytes
);

/// <summary>
/// Orchestrates the process of preparing and running video uploads.
/// </summary>
public interface IUploadOrchestrator
{
    /// <summary>
    /// Scans the directory for video files, checks against the upload log, 
    /// and prepares a list of UploadJobs with computed scheduled dates.
    /// </summary>
    /// <param name="directoryPath">The video directory path.</param>
    /// <param name="config">The upload configuration settings.</param>
    /// <returns>A list of pending upload jobs.</returns>
    IReadOnlyList<UploadJob> PrepareUploadBatch(string directoryPath, UploadConfiguration config);

    /// <summary>
    /// Executes the upload batch sequentially.
    /// </summary>
    /// <param name="jobs">The list of upload jobs to execute.</param>
    /// <param name="progress">The progress reporter for the batch.</param>
    /// <param name="ct">The cancellation token.</param>
    Task ExecuteUploadBatchAsync(
        IReadOnlyList<UploadJob> jobs, 
        IProgress<BatchProgress> progress, 
        CancellationToken ct
    );
}
