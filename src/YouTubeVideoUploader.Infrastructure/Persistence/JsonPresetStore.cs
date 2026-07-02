using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Infrastructure.Persistence;

/// <summary>
/// Persists configuration presets to a JSON file on disk.
/// </summary>
public class JsonPresetStore : IPresetStore
{
    private readonly string _presetsFilePath;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JsonPresetStore.
    /// </summary>
    /// <param name="presetsFilePath">The path to the JSON file where presets are stored.</param>
    public JsonPresetStore(string presetsFilePath)
    {
        if (string.IsNullOrWhiteSpace(presetsFilePath))
        {
            throw new ArgumentException("Presets file path cannot be null or empty.", nameof(presetsFilePath));
        }

        _presetsFilePath = presetsFilePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<Preset> GetAll()
    {
        lock (_lock)
        {
            if (!File.Exists(_presetsFilePath))
            {
                return Array.Empty<Preset>();
            }

            try
            {
                string json = File.ReadAllText(_presetsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Array.Empty<Preset>();
                }

                var list = JsonSerializer.Deserialize<List<Preset>>(json, _jsonOptions);
                return list ?? new List<Preset>();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load presets from file {Path}", _presetsFilePath);
                return Array.Empty<Preset>();
            }
        }
    }

    /// <inheritdoc />
    public Preset? GetById(Guid id)
    {
        var all = GetAll();
        foreach (var preset in all)
        {
            if (preset.Id == id)
            {
                return preset;
            }
        }
        return null;
    }

    /// <inheritdoc />
    public void Save(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        lock (_lock)
        {
            var all = new List<Preset>(GetAll());
            
            // Remove existing preset with same ID if exists
            int index = all.FindIndex(p => p.Id == preset.Id);
            if (index >= 0)
            {
                all[index] = preset;
            }
            else
            {
                all.Add(preset);
            }

            try
            {
                string? dir = Path.GetDirectoryName(_presetsFilePath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(all, _jsonOptions);
                File.WriteAllText(_presetsFilePath, json);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to save presets to file {Path}", _presetsFilePath);
                throw new InvalidOperationException("Failed to save preset to disk.", ex);
            }
        }
    }

    /// <inheritdoc />
    public void Delete(Guid id)
    {
        lock (_lock)
        {
            var all = new List<Preset>(GetAll());
            int index = all.FindIndex(p => p.Id == id);
            if (index >= 0)
            {
                all.RemoveAt(index);
                try
                {
                    string json = JsonSerializer.Serialize(all, _jsonOptions);
                    File.WriteAllText(_presetsFilePath, json);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Failed to delete preset from file {Path}", _presetsFilePath);
                    throw new InvalidOperationException("Failed to delete preset from disk.", ex);
                }
            }
        }
    }
}
