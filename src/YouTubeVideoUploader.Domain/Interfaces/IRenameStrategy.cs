using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Defines a strategy for generating new file names based on a template.
/// </summary>
public interface IRenameStrategy
{
    /// <summary>
    /// Gets the display name of the strategy (for UI selection).
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this strategy requires a name list to function.
    /// </summary>
    bool RequiresNameList { get; }

    /// <summary>
    /// Generates a new file name for the specified video file.
    /// </summary>
    /// <param name="file">The video file to rename.</param>
    /// <param name="index">The zero-based index of the file in the sorting order.</param>
    /// <param name="template">The rename template to use.</param>
    /// <returns>The generated file name with extension.</returns>
    string GenerateNewName(VideoFile file, int index, RenameTemplate template);
}
