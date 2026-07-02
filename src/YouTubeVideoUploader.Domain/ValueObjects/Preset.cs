using System;

namespace YouTubeVideoUploader.Domain.ValueObjects;

/// <summary>
/// Value object representing a saved profile/preset for upload configuration and/or renaming template.
/// </summary>
public record Preset
{
    /// <summary>
    /// Gets the unique identifier for the preset.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the name of the preset.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the upload configuration.
    /// </summary>
    public UploadConfiguration UploadConfig { get; init; } = new();

    /// <summary>
    /// Gets the rename template.
    /// </summary>
    public RenameTemplate RenameTemplate { get; init; } = new();
}
