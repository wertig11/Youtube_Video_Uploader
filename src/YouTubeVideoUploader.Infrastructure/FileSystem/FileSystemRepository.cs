using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;

namespace YouTubeVideoUploader.Infrastructure.FileSystem;

/// <summary>
/// Infrastructure implementation of the file system repository using System.IO.
/// </summary>
public class FileSystemRepository : IFileSystemRepository
{
    /// <inheritdoc />
    public IReadOnlyList<VideoFile> GetVideoFiles(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var directoryInfo = new DirectoryInfo(directoryPath);
        
        // Find all files matching supported extensions (case insensitive)
        var files = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly)
            .Where(f => VideoFile.SupportedExtensions.Contains(f.Extension))
            .Select(f => new VideoFile(
                f.FullName,
                f.Name,
                f.Extension,
                f.LastWriteTime,
                f.Length
            ))
            .ToList();

        return files;
    }

    /// <inheritdoc />
    public void RenameFile(VideoFile file, string newFileName)
    {
        ArgumentNullException.ThrowIfNull(file);
        
        if (string.IsNullOrWhiteSpace(newFileName))
        {
            throw new ArgumentException("New file name cannot be null or empty.", nameof(newFileName));
        }

        if (!File.Exists(file.FilePath))
        {
            throw new FileNotFoundException("Source file to rename does not exist.", file.FilePath);
        }

        string directory = Path.GetDirectoryName(file.FilePath) 
            ?? throw new InvalidOperationException($"Could not get directory from path: {file.FilePath}");

        string targetPath = Path.Combine(directory, newFileName);

        File.Move(file.FilePath, targetPath);
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return File.Exists(path);
    }
}
