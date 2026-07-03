using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Services;

/// <summary>
/// Service implementing file renaming preview, execution, and undo.
/// </summary>
public class RenameService : IRenameService
{
    private readonly IFileSystemRepository _fileSystemRepository;

    /// <summary>
    /// Initializes a new instance of the RenameService.
    /// </summary>
    /// <param name="fileSystemRepository">The file system repository.</param>
    public RenameService(IFileSystemRepository fileSystemRepository)
    {
        _fileSystemRepository = fileSystemRepository ?? throw new ArgumentNullException(nameof(fileSystemRepository));
    }

    /// <inheritdoc />
    public IReadOnlyList<RenamePair> PreviewRenames(string directoryPath, RenameTemplate template, IRenameStrategy strategy)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(strategy);

        // Get video files and sort by CreationDate ascending (matching PowerShell script)
        var files = _fileSystemRepository.GetVideoFiles(directoryPath)
            .OrderBy(f => f.CreationDate)
            .ToList();

        // Determine how many files to process
        int maxItemsToProcess = strategy.RequiresNameList && template.MaxItems > 0
            ? Math.Min(files.Count, template.MaxItems)
            : files.Count;

        var pairs = new List<RenamePair>();
        var proposedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < maxItemsToProcess; i++)
        {
            var file = files[i];
            string generatedName = strategy.GenerateNewName(file, i, template);

            // Ensure uniqueness in the folder and batch
            string uniqueName = GenerateUniqueFileName(directoryPath, generatedName, proposedNames);
            string newFullPath = Path.Combine(directoryPath, uniqueName);

            pairs.Add(new RenamePair(file, uniqueName, newFullPath));
            proposedNames.Add(uniqueName);
        }

        return pairs;
    }

    /// <inheritdoc />
    public IReadOnlyList<RenamePair> PreviewRenamesSelected(string directoryPath, IReadOnlyList<string> selectedFilePaths, RenameTemplate template, IRenameStrategy strategy)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentNullException.ThrowIfNull(selectedFilePaths);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(strategy);

        var files = _fileSystemRepository.GetVideoFilesFromPaths(selectedFilePaths)
            .OrderBy(f => f.CreationDate)
            .ToList();

        int maxItemsToProcess = strategy.RequiresNameList && template.MaxItems > 0
            ? Math.Min(files.Count, template.MaxItems)
            : files.Count;

        var pairs = new List<RenamePair>();
        var proposedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < maxItemsToProcess; i++)
        {
            var file = files[i];
            string generatedName = strategy.GenerateNewName(file, i, template);
            string uniqueName = GenerateUniqueFileName(directoryPath, generatedName, proposedNames);
            string newFullPath = Path.Combine(directoryPath, uniqueName);

            pairs.Add(new RenamePair(file, uniqueName, newFullPath));
            proposedNames.Add(uniqueName);
        }

        return pairs;
    }

    /// <inheritdoc />
    public void ExecuteRenames(IReadOnlyList<RenamePair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        foreach (var pair in pairs)
        {
            _fileSystemRepository.RenameFile(pair.File, pair.NewName);
        }
    }

    /// <inheritdoc />
    public void UndoRenames(IReadOnlyList<RenamePair> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        // Revert in reverse order to prevent collision issues
        for (int i = pairs.Count - 1; i >= 0; i--)
        {
            var pair = pairs[i];

            if (_fileSystemRepository.FileExists(pair.NewFullPath))
            {
                // Create a temporary VideoFile representation of the renamed file
                var tempFile = new VideoFile(
                    pair.NewFullPath,
                    pair.NewName,
                    Path.GetExtension(pair.NewName),
                    pair.File.CreationDate,
                    pair.File.FileSize
                );

                _fileSystemRepository.RenameFile(tempFile, pair.OldName);
            }
        }
    }

    private string GenerateUniqueFileName(string directoryPath, string fileName, HashSet<string> proposedNames)
    {
        string fullPath = Path.Combine(directoryPath, fileName);

        if (!_fileSystemRepository.FileExists(fullPath) && !proposedNames.Contains(fileName))
        {
            return fileName;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        int counter = 1;
        string newName;
        do
        {
            newName = $"{baseName} ({counter}){extension}";
            fullPath = Path.Combine(directoryPath, newName);
            counter++;
        }
        while (_fileSystemRepository.FileExists(fullPath) || proposedNames.Contains(newName));

        return newName;
    }
}
