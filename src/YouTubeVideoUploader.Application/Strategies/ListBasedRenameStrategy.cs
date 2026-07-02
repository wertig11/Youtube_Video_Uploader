using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Strategies;

/// <summary>
/// A renaming strategy that uses a list of names to substitute into a template.
/// </summary>
public class ListBasedRenameStrategy : IRenameStrategy
{
    /// <inheritdoc />
    public string DisplayName => "Rename by list (Doom style)";

    /// <inheritdoc />
    public bool RequiresNameList => true;

    /// <inheritdoc />
    public string GenerateNewName(VideoFile file, int index, RenameTemplate template)
    {
        string baseName = template.GenerateName(index);
        return $"{baseName}{file.Extension}";
    }
}
