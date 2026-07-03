using System;
using System.Collections.Generic;

namespace YouTubeVideoUploader.Domain.ValueObjects;

public class AppStateJob
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string CategoryId { get; set; } = "20";
    public string PrivacyStatus { get; set; } = "Private";
    public DateTime? ScheduledPublishDate { get; set; }
    public string PlaylistId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class AppState
{
    public string RenameFolderPath { get; set; } = string.Empty;
    public string RenameTemplatePattern { get; set; } = string.Empty;
    public string RenameNamesText { get; set; } = string.Empty;
    public string SelectedStrategyName { get; set; } = string.Empty;

    public string UploadFolderPath { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public string TagsText { get; set; } = string.Empty;
    public string DescriptionTemplate { get; set; } = string.Empty;
    public string CategoryId { get; set; } = "20";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public string PublishTimeText { get; set; } = string.Empty;
    public int IntervalDays { get; set; } = 1;
    public string SelectedUploadFilePaths { get; set; } = string.Empty; // Semi-colon separated paths

    public string ClientSecretPath { get; set; } = string.Empty;
    public string Language { get; set; } = "en";

    public List<AppStateJob> ActiveUploadJobs { get; set; } = new();
}
