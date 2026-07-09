using System;
using System.Threading;
using System.Threading.Tasks;
using YouTubeVideoUploader.Domain.Entities;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Represents the progress of a video upload.
/// </summary>
public record UploadProgress(long BytesSent, long TotalBytes);

/// <summary>
/// Defines interactions with the YouTube Data API.
/// </summary>
public interface IYouTubeGateway
{
    /// <summary>
    /// Uploads a video to YouTube asynchronously.
    /// </summary>
    /// <param name="job">The upload job details.</param>
    /// <param name="progress">The progress reporter.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The uploaded video's YouTube ID.</returns>
    Task<string> UploadVideoAsync(UploadJob job, IProgress<UploadProgress> progress, CancellationToken ct);

    /// <summary>
    /// Adds a video to a specific playlist.
    /// </summary>
    /// <param name="videoId">The YouTube ID of the video.</param>
    /// <param name="playlistId">The target playlist ID.</param>
    /// <param name="ct">The cancellation token.</param>
    Task AddToPlaylistAsync(string videoId, string playlistId, CancellationToken ct);

    /// <summary>
    /// Validates the current credentials and tries to authenticate.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if authenticated; otherwise, false.</returns>
    Task<bool> ValidateCredentialsAsync(CancellationToken ct);
}
