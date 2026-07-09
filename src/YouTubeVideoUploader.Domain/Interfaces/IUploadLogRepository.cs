using System.Collections.Generic;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Defines persistence operations for tracking already uploaded files to prevent duplicates.
/// </summary>
public interface IUploadLogRepository
{
    /// <summary>
    /// Gets the names of all files that have already been uploaded.
    /// </summary>
    /// <returns>A read-only set of uploaded filenames.</returns>
    IReadOnlySet<string> GetUploadedFileNames();

    /// <summary>
    /// Logs a successful file upload.
    /// </summary>
    /// <param name="fileName">The local file name.</param>
    /// <param name="youtubeVideoId">The uploaded video ID.</param>
    void LogUploadedFile(string fileName, string youtubeVideoId);
}
