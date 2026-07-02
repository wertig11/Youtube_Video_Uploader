using System;
using System.Collections.Generic;

namespace YouTubeVideoUploader.Domain.ValueObjects;

/// <summary>
/// Value object representing a template for renaming video files.
/// </summary>
public record RenameTemplate
{
    /// <summary>
    /// Gets the template pattern string (e.g., "Master levels for doom 2 Coop #{Number} - {LevelName}").
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of names to substitute into the template (e.g. Doom level names).
    /// </summary>
    public IReadOnlyList<string> Names { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the maximum number of items that can be renamed using this template's name list.
    /// </summary>
    public int MaxItems => Names.Count;

    /// <summary>
    /// Generates a new name for a file based on its index.
    /// </summary>
    /// <param name="index">The zero-based index of the file.</param>
    /// <returns>The generated file name without extension.</returns>
    public string GenerateName(int index)
    {
        string result = Pattern;

        // Replace number placeholder (1-based index)
        int number = index + 1;
        result = result.Replace("{Number}", number.ToString(), StringComparison.OrdinalIgnoreCase);

        // Replace level name placeholder if index is within range of the list
        string levelName = index < Names.Count ? Names[index] : $"Level {number}";
        result = result.Replace("{LevelName}", levelName, StringComparison.OrdinalIgnoreCase);

        return result;
    }
}
