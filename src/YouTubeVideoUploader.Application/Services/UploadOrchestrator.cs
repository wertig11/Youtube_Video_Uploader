using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Services;

/// <summary>
/// Orchestrates preparing and executing a batch of video uploads to YouTube.
/// </summary>
public class UploadOrchestrator : IUploadOrchestrator
{
    private readonly IFileSystemRepository _fileSystemRepository;
    private readonly IUploadLogRepository _uploadLogRepository;
    private readonly IYouTubeGateway _youtubeGateway;
    private readonly IUploadJobFactory _uploadJobFactory;
    private readonly ScheduleCalculator _scheduleCalculator;

    /// <summary>
    /// Initializes a new instance of the UploadOrchestrator.
    /// </summary>
    public UploadOrchestrator(
        IFileSystemRepository fileSystemRepository,
        IUploadLogRepository uploadLogRepository,
        IYouTubeGateway youtubeGateway,
        IUploadJobFactory uploadJobFactory,
        ScheduleCalculator scheduleCalculator)
    {
        _fileSystemRepository = fileSystemRepository ?? throw new ArgumentNullException(nameof(fileSystemRepository));
        _uploadLogRepository = uploadLogRepository ?? throw new ArgumentNullException(nameof(uploadLogRepository));
        _youtubeGateway = youtubeGateway ?? throw new ArgumentNullException(nameof(youtubeGateway));
        _uploadJobFactory = uploadJobFactory ?? throw new ArgumentNullException(nameof(uploadJobFactory));
        _scheduleCalculator = scheduleCalculator ?? throw new ArgumentNullException(nameof(scheduleCalculator));
    }

    /// <inheritdoc />
    public IReadOnlyList<UploadJob> PrepareUploadBatch(string directoryPath, UploadConfiguration config)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(config);

        // Fetch video files
        var files = _fileSystemRepository.GetVideoFiles(directoryPath);

        // Sort files naturally by name (matching the Python script's natural_sort_key)
        var sortedFiles = files
            .OrderBy(f => f.FileName, new NaturalStringComparer())
            .ToList();

        var uploadedLogs = _uploadLogRepository.GetUploadedFileNames();
        var jobs = new List<UploadJob>();

        // Calculate publish dates for all files in order
        var publishDates = _scheduleCalculator.CalculatePublishDates(sortedFiles.Count, config.Schedule);

        for (int i = 0; i < sortedFiles.Count; i++)
        {
            var file = sortedFiles[i];
            var publishDate = publishDates[i];

            // Create the job
            var job = _uploadJobFactory.CreateJob(file, config, publishDate);

            // If already uploaded, mark it as skipped
            if (uploadedLogs.Contains(file.FileName))
            {
                job.MarkSkipped();
            }

            jobs.Add(job);
        }

        return jobs;
    }

    /// <inheritdoc />
    public async Task ExecuteUploadBatchAsync(
        IReadOnlyList<UploadJob> jobs, 
        IProgress<BatchProgress> progress, 
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(progress);

        int totalCount = jobs.Count(j => j.Status != Domain.Enums.UploadStatus.Skipped);
        int completedCount = 0;

        foreach (var job in jobs)
        {
            ct.ThrowIfCancellationRequested();

            if (job.Status == Domain.Enums.UploadStatus.Skipped || job.Status == Domain.Enums.UploadStatus.Completed)
            {
                continue;
            }

            job.MarkUploading();

            // Setup nested progress reporter for the individual video upload
            var videoProgress = new Progress<UploadProgress>(p =>
            {
                progress.Report(new BatchProgress(
                    completedCount,
                    totalCount,
                    job.Title,
                    p.BytesSent,
                    p.TotalBytes
                ));
            });

            try
            {
                // 1. Upload Video
                string videoId = await _youtubeGateway.UploadVideoAsync(job, videoProgress, ct);
                
                // 2. Add to Playlist if specified
                if (!string.IsNullOrWhiteSpace(job.PlaylistId))
                {
                    await _youtubeGateway.AddToPlaylistAsync(videoId, job.PlaylistId, ct);
                }

                // 3. Mark job complete and log
                job.MarkCompleted(videoId);
                _uploadLogRepository.LogUploadedFile(job.VideoFile.FileName, videoId);
                completedCount++;

                // Trigger final completion progress for this video
                progress.Report(new BatchProgress(
                    completedCount,
                    totalCount,
                    job.Title,
                    job.VideoFile.FileSize,
                    job.VideoFile.FileSize
                ));
            }
            catch (Exception ex)
            {
                job.MarkFailed(ex.Message);
                
                // Report failure (current progress resets to 0/Failed)
                progress.Report(new BatchProgress(
                    completedCount,
                    totalCount,
                    job.Title,
                    0,
                    job.VideoFile.FileSize
                ));

                // Rethrow or continue depending on workflow. In this app, we stop the batch on first major upload error
                throw;
            }
        }
    }
}

/// <summary>
/// A comparer that performs a natural sort (alphanumeric sort) on strings.
/// (e.g. "Doom Coop #2" comes before "Doom Coop #10").
/// </summary>
public class NaturalStringComparer : IComparer<string>
{
    private static readonly Regex ChunkRegex = new(@"(\d+)|(\D+)", RegexOptions.Compiled);

    /// <inheritdoc />
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var xChunks = ChunkRegex.Matches(x).Select(m => m.Value).ToList();
        var yChunks = ChunkRegex.Matches(y).Select(m => m.Value).ToList();

        int max = Math.Min(xChunks.Count, yChunks.Count);
        for (int i = 0; i < max; i++)
        {
            string cx = xChunks[i];
            string cy = yChunks[i];

            if (char.IsDigit(cx[0]) && char.IsDigit(cy[0]))
            {
                if (long.TryParse(cx, out long valX) && long.TryParse(cy, out long valY))
                {
                    int comp = valX.CompareTo(valY);
                    if (comp != 0) return comp;
                }
            }

            int stringComp = string.Compare(cx, cy, StringComparison.OrdinalIgnoreCase);
            if (stringComp != 0) return stringComp;
        }

        return xChunks.Count.CompareTo(yChunks.Count);
    }
}
