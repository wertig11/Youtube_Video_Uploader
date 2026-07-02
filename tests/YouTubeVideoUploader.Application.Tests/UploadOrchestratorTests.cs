using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YouTubeVideoUploader.Application.Factories;
using YouTubeVideoUploader.Application.Services;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Enums;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Tests;

public class UploadOrchestratorTests
{
    [Fact]
    public void TagGenerator_ShouldCreateDeduplicatedTags()
    {
        // Arrange
        var generator = new TagGenerator();
        string title = "Master levels for doom 2 Coop #1 - Canyon Attack";
        var staticTags = new[] { "gameplay", "Doom" };

        // Act
        var tags = generator.GenerateTags(title, staticTags);

        // Assert
        // Expected cleaned title words: "canyon", "attack"
        // Static tags: "gameplay", "Doom" -> "doom" is deduplicated or matches
        Assert.Contains("gameplay", tags);
        Assert.Contains("canyon attack", tags);
        Assert.Contains("canyon", tags);
        Assert.Contains("attack", tags);
        Assert.DoesNotContain("Master levels for doom 2", tags);
        Assert.DoesNotContain("Coop", tags);
    }

    [Fact]
    public void ScheduleCalculator_ShouldCalculateIntervalsCorrectly()
    {
        // Arrange
        var calculator = new ScheduleCalculator();
        var schedule = new PublishSchedule
        {
            StartDate = new DateOnly(2026, 7, 1),
            PublishTime = new TimeOnly(12, 0, 0),
            IntervalDays = 2 // Every 2 days
        };

        // Act
        var dates = calculator.CalculatePublishDates(3, schedule);

        // Assert
        Assert.Equal(3, dates.Count);
        Assert.Equal(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc), dates[0]);
        Assert.Equal(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc), dates[1]);
        Assert.Equal(new DateTime(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc), dates[2]);
    }

    private class MockFileSystemRepository : IFileSystemRepository
    {
        public IReadOnlyList<VideoFile> GetVideoFiles(string directoryPath)
        {
            return new List<VideoFile>
            {
                new("C:\\vids\\1.mp4", "1.mp4", ".mp4", DateTime.Now, 100),
                new("C:\\vids\\2.mp4", "2.mp4", ".mp4", DateTime.Now, 100)
            };
        }
        public void RenameFile(VideoFile file, string newFileName) { }
        public bool FileExists(string path) => false;
    }

    private class MockUploadLogRepository : IUploadLogRepository
    {
        public IReadOnlySet<string> GetUploadedFileNames()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.mp4" };
        }
        public void LogUploadedFile(string fileName, string youtubeVideoId) { }
    }

    private class DummyYouTubeGateway : IYouTubeGateway
    {
        public Task<string> UploadVideoAsync(UploadJob job, IProgress<UploadProgress> progress, CancellationToken ct) => Task.FromResult("vid123");
        public Task AddToPlaylistAsync(string videoId, string playlistId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ValidateCredentialsAsync(CancellationToken ct) => Task.FromResult(true);
    }

    [Fact]
    public void UploadOrchestrator_PrepareBatch_ShouldSkipAlreadyUploaded()
    {
        // Arrange
        var fileRepo = new MockFileSystemRepository();
        var logRepo = new MockUploadLogRepository();
        var ytGateway = new DummyYouTubeGateway();
        var tagGen = new TagGenerator();
        var factory = new UploadJobFactory(tagGen);
        var calculator = new ScheduleCalculator();

        var orchestrator = new UploadOrchestrator(fileRepo, logRepo, ytGateway, factory, calculator);

        var config = new UploadConfiguration
        {
            GameName = "Doom",
            Schedule = new PublishSchedule
            {
                StartDate = new DateOnly(2026, 7, 1),
                PublishTime = new TimeOnly(12, 0, 0)
            }
        };

        // Act
        var batch = orchestrator.PrepareUploadBatch("C:\\vids", config);

        // Assert
        Assert.Equal(2, batch.Count);
        
        // 1.mp4 is already uploaded, should be marked Skipped
        Assert.Equal(UploadStatus.Skipped, batch.First(j => j.VideoFile.FileName == "1.mp4").Status);
        
        // 2.mp4 is not uploaded, should be Pending
        Assert.Equal(UploadStatus.Pending, batch.First(j => j.VideoFile.FileName == "2.mp4").Status);
    }
}
