namespace YouTubeVideoUploader.Domain.Enums;

/// <summary>
/// Represents the various statuses that a YouTube video upload can have during its lifecycle.
/// </summary>
public enum UploadStatus
{
    /// <summary>
    /// The upload has been initialized but no upload process has started yet.
    /// </summary>
    Pending,

    /// <summary>
    /// The video is currently being uploaded to YouTube.
    /// </summary>
    Uploading,

    /// <summary>
    /// The video has been successfully uploaded and processed by YouTube.
    /// </summary>
    Completed,

    /// <summary>
    /// The upload process encountered an error and was not completed successfully.
    /// </summary>
    Failed,

    /// <summary>
    /// The upload was intentionally bypassed, e.g., due to user request or content policy check.
    /// </summary>
    Skipped
}
