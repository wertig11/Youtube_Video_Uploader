using System.ComponentModel;
using System.Runtime.CompilerServices;
using YouTubeVideoUploader.Domain.Enums;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Entities;

/// <summary>
/// Represents a job to upload a video to YouTube.
/// </summary>
public class UploadJob : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private readonly VideoFile _videoFile;

    /// <summary>
    /// Gets the video file associated with this upload job.
    /// </summary>
    public VideoFile VideoFile => _videoFile;

    /// <summary>
    /// Gets or sets the title of the video.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the video.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the list of tags for the video.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the category ID for the video.
    /// </summary>
    public string CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the privacy status of the video.
    /// </summary>
    public PrivacyStatus PrivacyStatus { get; set; }

    /// <summary>
    /// Gets or sets the scheduled publish date (optional).
    /// </summary>
    public DateTime? ScheduledPublishDate { get; set; }

    /// <summary>
    /// Gets or sets the playlist ID to which the video should be added.
    /// </summary>
    public string PlaylistId { get; set; }

    /// <summary>
    /// Gets the current status of the upload job.
    /// </summary>
    public UploadStatus Status { get; private set; } = UploadStatus.Pending;

    /// <summary>
    /// Gets the YouTube video ID after successful upload, or null if not uploaded yet.
    /// </summary>
    public string? YouTubeVideoId { get; private set; }

    /// <summary>
    /// Gets the error message if the upload job failed, or null otherwise.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadJob"/> class.
    /// </summary>
    /// <param name="videoFile">The video file to upload. Cannot be null.</param>
    /// <param name="title">The title of the video. Cannot be null or empty.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="videoFile"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="title"/> is null or empty.</exception>
    public UploadJob(VideoFile videoFile, string title)
    {
        ArgumentNullException.ThrowIfNull(videoFile);
        ArgumentException.ThrowIfNullOrEmpty(title);

        _videoFile = videoFile;
        Title = title;
        Description = string.Empty;
        Tags = Array.Empty<string>();
        CategoryId = string.Empty;
        PrivacyStatus = PrivacyStatus.Private;
        ScheduledPublishDate = null;
        PlaylistId = string.Empty;
    }

    /// <summary>
    /// Marks the upload job as uploading.
    /// </summary>
    public void MarkUploading()
    {
        Status = UploadStatus.Uploading;
        OnPropertyChanged(nameof(Status));
    }

    /// <summary>
    /// Marks the upload job as completed with the specified YouTube video ID.
    /// </summary>
    /// <param name="videoId">The YouTube video ID. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="videoId"/> is null.</exception>
    public void MarkCompleted(string videoId)
    {
        ArgumentNullException.ThrowIfNull(videoId);
        Status = UploadStatus.Completed;
        YouTubeVideoId = videoId;
        ErrorMessage = null;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(YouTubeVideoId));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    /// <summary>
    /// Marks the upload job as failed with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public void MarkFailed(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Status = UploadStatus.Failed;
        ErrorMessage = error;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    /// <summary>
    /// Marks the upload job as skipped.
    /// </summary>
    public void MarkSkipped()
    {
        Status = UploadStatus.Skipped;
        OnPropertyChanged(nameof(Status));
    }
}
