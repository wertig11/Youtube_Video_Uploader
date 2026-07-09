using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using DomainUploadProgress = YouTubeVideoUploader.Domain.Interfaces.UploadProgress;

namespace YouTubeVideoUploader.Infrastructure.YouTube;

/// <summary>
/// Adapter implementing IYouTubeGateway using the official Google YouTube API SDK.
/// </summary>
public class YouTubeApiAdapter : IYouTubeGateway
{
    private readonly GoogleAuthService _authService;
    private const string ApplicationName = "YouTubeVideoUploader";

    /// <summary>
    /// Initializes a new instance of the YouTubeApiAdapter.
    /// </summary>
    /// <param name="authService">The Google authentication service.</param>
    public YouTubeApiAdapter(IAuthenticationService authService)
    {
        _authService = (authService as GoogleAuthService) 
            ?? throw new ArgumentException("Authentication service must be of type GoogleAuthService.", nameof(authService));
    }

    private YouTubeService GetService()
    {
        if (_authService.Credential == null)
        {
            throw new InvalidOperationException("User is not authenticated. Please authenticate first.");
        }

        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _authService.Credential,
            ApplicationName = ApplicationName
        });
    }

    /// <inheritdoc />
    public async Task<string> UploadVideoAsync(UploadJob job, IProgress<DomainUploadProgress> progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(progress);

        var youtubeService = GetService();

        var video = new Video
        {
            Snippet = new VideoSnippet
            {
                Title = job.Title,
                Description = job.Description,
                Tags = job.Tags?.ToList(),
                CategoryId = job.CategoryId
            },
            Status = new VideoStatus
            {
                PrivacyStatus = job.PrivacyStatus.ToString().ToLowerInvariant()
            }
        };

        // Scheduled publishing requires the status to be Private and PublishAt to be set
        if (job.ScheduledPublishDate.HasValue)
        {
            video.Status.PrivacyStatus = "private";
            // Convert to DateTimeOffset and format as ISO 8601 string Z
            video.Status.PublishAtDateTimeOffset = new DateTimeOffset(job.ScheduledPublishDate.Value, TimeSpan.Zero);
        }

        string contentType = job.VideoFile.Extension.ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };

        var fileStream = new FileStream(job.VideoFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        // Chunk size: -1 lets the SDK decide or chunksize can be set. Resumable = true.
        var insertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, contentType);
        insertRequest.ChunkSize = ResumableUpload.MinimumChunkSize * 4; // 1MB chunks

        var tcs = new TaskCompletionSource<string>();

        // Wire up progress reporting
        insertRequest.ProgressChanged += (IUploadProgress uploadProgress) =>
        {
            switch (uploadProgress.Status)
            {
                case UploadStatus.Uploading:
                    progress.Report(new DomainUploadProgress(uploadProgress.BytesSent, job.VideoFile.FileSize));
                    break;

                case UploadStatus.Failed:
                    tcs.TrySetException(uploadProgress.Exception ?? new Exception("YouTube upload failed with unknown error."));
                    break;
            }
        };

        insertRequest.ResponseReceived += (Video uploadedVideo) =>
        {
            tcs.TrySetResult(uploadedVideo.Id);
        };

        // Handle cancellation
        using (ct.Register(() => 
        {
            fileStream.Dispose();
            tcs.TrySetCanceled(ct);
        }))
        {
            try
            {
                // Run the upload on a background thread
                var uploadTask = insertRequest.UploadAsync(ct);
                
                // Wait for the completion event to fire and set result on tcs, or task failure
                await Task.WhenAny(uploadTask, tcs.Task);

                if (tcs.Task.IsCompleted)
                {
                    return await tcs.Task;
                }
                
                if (uploadTask.IsFaulted && uploadTask.Exception != null)
                {
                    throw uploadTask.Exception.InnerException ?? uploadTask.Exception;
                }

                throw new InvalidOperationException("Upload completed without receiving a YouTube ID response.");
            }
            finally
            {
                fileStream.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public async Task AddToPlaylistAsync(string videoId, string playlistId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(videoId);
        ArgumentException.ThrowIfNullOrEmpty(playlistId);

        var youtubeService = GetService();

        var playlistItem = new PlaylistItem
        {
            Snippet = new PlaylistItemSnippet
            {
                PlaylistId = playlistId,
                ResourceId = new ResourceId
                {
                    Kind = "youtube#video",
                    VideoId = videoId
                }
            }
        };

        var request = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
        await request.ExecuteAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateCredentialsAsync(CancellationToken ct)
    {
        try
        {
            var youtubeService = GetService();
            // Perform a minimal API read call (fetch user channel) to verify authentication works
            var request = youtubeService.Channels.List("id");
            request.Mine = true;
            var response = await request.ExecuteAsync(ct);
            return response != null;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Credential validation failed.");
            return false;
        }
    }
}
