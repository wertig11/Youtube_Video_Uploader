using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Service for previewing, executing, and undoing file renaming operations.
/// </summary>
public interface IRenameService
{
    /// <summary>
    /// Previews the rename operations without executing them.
    /// </summary>
    /// <param name="directoryPath">The target directory.</param>
    /// <param name="template">The template with names and pattern.</param>
    /// <param name="strategy">The rename strategy to apply.</param>
    /// <returns>A read-only list of proposed rename operations.</returns>
    IReadOnlyList<RenamePair> PreviewRenames(string directoryPath, RenameTemplate template, IRenameStrategy strategy);

    /// <summary>
    /// Executes the rename operations for the given pairs.
    /// </summary>
    /// <param name="pairs">The rename pairs to execute.</param>
    void ExecuteRenames(IReadOnlyList<RenamePair> pairs);

    /// <summary>
    /// Reverts a previously executed set of rename operations.
    /// </summary>
    /// <param name="pairs">The rename pairs to undo (the same pairs returned by PreviewRenames).</param>
    void UndoRenames(IReadOnlyList<RenamePair> pairs);
}
