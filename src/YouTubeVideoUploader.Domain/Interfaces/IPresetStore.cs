using System;
using System.Collections.Generic;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Domain.Interfaces;

/// <summary>
/// Defines storage operations for saved configuration presets.
/// </summary>
public interface IPresetStore
{
    /// <summary>
    /// Gets all saved presets.
    /// </summary>
    /// <returns>A read-only list of presets.</returns>
    IReadOnlyList<Preset> GetAll();

    /// <summary>
    /// Gets a preset by its unique ID.
    /// </summary>
    /// <param name="id">The preset ID.</param>
    /// <returns>The preset if found; otherwise, null.</returns>
    Preset? GetById(Guid id);

    /// <summary>
    /// Saves or updates a preset.
    /// </summary>
    /// <param name="preset">The preset to save.</param>
    void Save(Preset preset);

    /// <summary>
    /// Deletes a preset by its unique ID.
    /// </summary>
    /// <param name="id">The preset ID.</param>
    void Delete(Guid id);
}
