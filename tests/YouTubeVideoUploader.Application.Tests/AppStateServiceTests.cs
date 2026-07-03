using System;
using System.IO;
using Xunit;
using YouTubeVideoUploader.Domain.ValueObjects;
using YouTubeVideoUploader.Infrastructure.Persistence;

namespace YouTubeVideoUploader.Application.Tests;

public class AppStateServiceTests
{
    [Fact]
    public void LoadState_ReturnsDefaultState_WhenFileDoesNotExist()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        var service = new JsonAppStateService(tempFile);

        try
        {
            // Act
            var state = service.LoadState();

            // Assert
            Assert.NotNull(state);
            Assert.Empty(state.RenameFolderPath);
            Assert.Empty(state.ActiveUploadJobs);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoadState_SavesAndRestoresCorrectly()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        var service = new JsonAppStateService(tempFile);

        var originalState = new AppState
        {
            RenameFolderPath = @"C:\TestPath",
            RenameTemplatePattern = "Pattern-{Number}",
            Language = "uk",
            ActiveUploadJobs = new()
            {
                new AppStateJob
                {
                    FilePath = @"C:\TestPath\video.mp4",
                    Title = "Test Video",
                    Status = "Completed",
                    YouTubeVideoId = "dQw4w9WgXcQ"
                }
            }
        };

        try
        {
            // Act
            service.SaveState(originalState);
            var loadedState = service.LoadState();

            // Assert
            Assert.NotNull(loadedState);
            Assert.Equal(originalState.RenameFolderPath, loadedState.RenameFolderPath);
            Assert.Equal(originalState.RenameTemplatePattern, loadedState.RenameTemplatePattern);
            Assert.Equal(originalState.Language, loadedState.Language);
            Assert.Single(loadedState.ActiveUploadJobs);
            Assert.Equal("Completed", loadedState.ActiveUploadJobs[0].Status);
            Assert.Equal("dQw4w9WgXcQ", loadedState.ActiveUploadJobs[0].YouTubeVideoId);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
