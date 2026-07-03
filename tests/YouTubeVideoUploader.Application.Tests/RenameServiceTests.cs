using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YouTubeVideoUploader.Application.Services;
using YouTubeVideoUploader.Application.Strategies;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Tests;

public class RenameServiceTests
{
    private class MockFileSystemRepository : IFileSystemRepository
    {
        public List<VideoFile> Files { get; set; } = new();
        public List<(VideoFile File, string NewName)> Renamed { get; } = new();

        public IReadOnlyList<VideoFile> GetVideoFiles(string directoryPath) => Files;

        public IReadOnlyList<VideoFile> GetVideoFilesFromPaths(IReadOnlyList<string> filePaths) => 
            Files.Where(f => filePaths.Contains(f.FilePath, StringComparer.OrdinalIgnoreCase)).ToList();

        public void RenameFile(VideoFile file, string newFileName)
        {
            Renamed.Add((file, newFileName));
        }

        public bool FileExists(string path)
        {
            // Simple mock: check if any file in our lists matches
            return Files.Any(f => f.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)) ||
                   Renamed.Any(r => r.NewName.Equals(System.IO.Path.GetFileName(path), StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ListBasedRenameStrategy_ShouldSubstituteCorrectly()
    {
        // Arrange
        var strategy = new ListBasedRenameStrategy();
        var file = new VideoFile("C:\\videos\\video1.mkv", "video1.mkv", ".mkv", DateTime.Now, 100);
        var template = new RenameTemplate
        {
            Pattern = "Doom Coop #{Number} - {LevelName}",
            Names = new[] { "Attack", "Canyon" }
        };

        // Act
        string newName = strategy.GenerateNewName(file, 0, template);

        // Assert
        Assert.Equal("Doom Coop #1 - Attack.mkv", newName);
    }

    [Fact]
    public void DateBasedRenameStrategy_ShouldSubstituteDateCorrectly()
    {
        // Arrange
        var strategy = new DateBasedRenameStrategy();
        var creationDate = new DateTime(2026, 6, 30, 9, 30, 0);
        var file = new VideoFile("C:\\videos\\video1.mkv", "video1.mkv", ".mkv", creationDate, 100);
        var template = new RenameTemplate
        {
            Pattern = "{GameName} - {Date:yyyy-MM-dd_HH-mm}"
        };

        // Act
        string newName = strategy.GenerateNewName(file, 0, template);

        // Assert
        Assert.Equal("{GameName} - 2026-06-30_09-30.mkv", newName);
    }

    [Fact]
    public void RenameService_PreviewRenames_ShouldSortAndHandleDuplicates()
    {
        // Arrange
        var mockRepo = new MockFileSystemRepository();
        var date1 = new DateTime(2026, 6, 30, 9, 0, 0);
        var date2 = new DateTime(2026, 6, 30, 8, 0, 0); // Older file, should be first
        
        mockRepo.Files.Add(new VideoFile("C:\\vids\\fileA.mp4", "fileA.mp4", ".mp4", date1, 1000));
        mockRepo.Files.Add(new VideoFile("C:\\vids\\fileB.mp4", "fileB.mp4", ".mp4", date2, 2000));

        var service = new RenameService(mockRepo);
        var template = new RenameTemplate
        {
            Pattern = "Video #{Number}",
            Names = Array.Empty<string>() // not needed for date strategy
        };
        var strategy = new DateBasedRenameStrategy(); // doesn't require names

        // Act
        var preview = service.PreviewRenames("C:\\vids", template, strategy);

        // Assert
        Assert.Equal(2, preview.Count);
        
        // Older file fileB should be Video #1
        Assert.Equal("fileB.mp4", preview[0].OldName);
        Assert.Equal("Video #1.mp4", preview[0].NewName);

        // Newer file fileA should be Video #2
        Assert.Equal("fileA.mp4", preview[1].OldName);
        Assert.Equal("Video #2.mp4", preview[1].NewName);
    }
}
