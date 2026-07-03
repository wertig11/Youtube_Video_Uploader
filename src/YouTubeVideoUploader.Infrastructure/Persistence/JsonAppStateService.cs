using System;
using System.IO;
using System.Text.Json;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Infrastructure.Persistence;

public class JsonAppStateService : IAppStateService
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public JsonAppStateService(string filePath)
    {
        _filePath = filePath;
    }

    public AppState LoadState()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    var state = JsonSerializer.Deserialize<AppState>(json);
                    return state ?? new AppState();
                }
            }
            catch
            {
                // Fallback to default state on error
            }
            return new AppState();
        }
    }

    public void SaveState(AppState state)
    {
        if (state == null) return;
        lock (_lock)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Silently ignore saving errors to prevent crash
            }
        }
    }
}
