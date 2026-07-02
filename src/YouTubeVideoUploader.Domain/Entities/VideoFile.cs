using System;
using System.Collections.Generic;

namespace YouTubeVideoUploader.Domain.Entities;

/// <summary>
/// Represents a video file entity with metadata.
/// </summary>
public class VideoFile
{
    /// <summary>
    /// Gets the full path to the video file.
    /// </summary>
    public string FilePath { get; private init; }

    /// <summary>
    /// Gets the name of the video file (including extension).
    /// </summary>
    public string FileName { get; private init; }

    /// <summary>
    /// Gets the file extension (e.g., ".mp4").
    /// </summary>
    public string Extension { get; private init; }

    /// <summary>
    /// Gets the creation date of the video file.
    /// </summary>
    public DateTime CreationDate { get; private init; }

    /// <summary>
    /// Gets the size of the video file in bytes.
    /// </summary>
    public long FileSize { get; private init; }

    /// <summary>
    /// Defines the collection of supported video file extensions.
    /// </summary>
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".mov" };

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFile"/> class.
    /// </summary>
    /// <param name="filePath">The full path to the video file.</param>
    /// <param name="fileName">The name of the video file.</param>
    /// <param name="extension">The file extension.</param>
    /// <param name="creationDate">The creation date of the video file.</param>
    /// <param name="fileSize">The size of the video file in bytes.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    public VideoFile(string filePath, string fileName, string extension, DateTime creationDate, long fileSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        FilePath = filePath;
        FileName = fileName;
        Extension = extension;
        CreationDate = creationDate;
        FileSize = fileSize;
    }

    /// <summary>
    /// Determines whether the video file's extension is supported.
    /// </summary>
    /// <returns><c>true</c> if the extension is supported; otherwise, <c>false</c>.</returns>
    public bool IsSupported()
    {
        return SupportedExtensions.Contains(Extension);
    }
}
