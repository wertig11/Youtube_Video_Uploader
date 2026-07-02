using System.Collections.Generic;
using YouTubeVideoUploader.Domain.Entities;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Defines file system operations needed for video file discovery and renaming.
/// </summary>
public interface IFileSystemRepository
{
    /// <summary>
    /// Gets all supported video files from the specified directory.
    /// </summary>
    /// <param name="directoryPath">The absolute path to the directory.</param>
    /// <returns>A read-only list of video files.</returns>
    IReadOnlyList<VideoFile> GetVideoFiles(string directoryPath);

    /// <summary>
    /// Renames a video file on disk.
    /// </summary>
    /// <param name="file">The video file entity.</param>
    /// <param name="newFileName">The new file name (including extension).</param>
    void RenameFile(VideoFile file, string newFileName);

    /// <summary>
    /// Checks if a file exists at the specified literal path.
    /// </summary>
    /// <param name="path">The absolute path to the file.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    bool FileExists(string path);
}
