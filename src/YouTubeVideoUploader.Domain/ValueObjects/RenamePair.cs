using YouTubeVideoUploader.Domain.Entities;

namespace YouTubeVideoUploader.Domain.ValueObjects;

/// <summary>
/// Represents a proposed rename operation for a file.
/// </summary>
public record RenamePair
{
    /// <summary>
    /// Gets the video file being renamed.
    /// </summary>
    public VideoFile File { get; init; }

    /// <summary>
    /// Gets the new name of the file (including extension).
    /// </summary>
    public string NewName { get; init; }

    /// <summary>
    /// Gets the original name of the file.
    /// </summary>
    public string OldName => File.FileName;

    /// <summary>
    /// Gets a value indicating whether the filename actually changed.
    /// </summary>
    public bool IsNameChanged => !string.Equals(OldName, NewName, System.StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the target file path.
    /// </summary>
    public string NewFullPath { get; init; }

    /// <summary>
    /// Gets the original file path.
    /// </summary>
    public string OldFullPath => File.FilePath;

    /// <summary>
    /// Initializes a new instance of the RenamePair record.
    /// </summary>
    public RenamePair(VideoFile file, string newName, string newFullPath)
    {
        File = file;
        NewName = newName;
        NewFullPath = newFullPath;
    }
}
