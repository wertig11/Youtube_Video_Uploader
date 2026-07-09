using System;
using System.Collections.Generic;
using System.IO;
using YouTubeVideoUploader.Domain.Interfaces;

namespace YouTubeVideoUploader.Infrastructure.Persistence;

/// <summary>
/// Persists the list of uploaded filenames in a plain text file (one per line)
/// to maintain 100% backward compatibility with the existing Python script's log file.
/// </summary>
public class TxtUploadLogRepository : IUploadLogRepository
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the TxtUploadLogRepository.
    /// </summary>
    /// <param name="logFilePath">The path to the text log file.</param>
    public TxtUploadLogRepository(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            throw new ArgumentException("Log file path cannot be null or empty.", nameof(logFilePath));
        }

        _logFilePath = logFilePath;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetUploadedFileNames()
    {
        lock (_lock)
        {
            var uploadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(_logFilePath))
            {
                try
                {
                    var lines = File.ReadAllLines(_logFilePath);
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            uploadedFiles.Add(trimmed);
                        }
                    }
                }
                catch (IOException ex)
                {
                    // Log or handle error appropriately. In this app, we return whatever we successfully read or empty
                    Serilog.Log.Error(ex, "Failed to read upload log file: {Path}", _logFilePath);
                }
            }

            return uploadedFiles;
        }
    }

    /// <inheritdoc />
    public void LogUploadedFile(string fileName, string youtubeVideoId)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        lock (_lock)
        {
            try
            {
                // Ensure directory exists
                string? dir = Path.GetDirectoryName(_logFilePath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Append filename to log (matching python behavior)
                File.AppendAllText(_logFilePath, fileName + Environment.NewLine);
            }
            catch (IOException ex)
            {
                Serilog.Log.Error(ex, "Failed to write to upload log file: {Path}", _logFilePath);
            }
        }
    }
}
