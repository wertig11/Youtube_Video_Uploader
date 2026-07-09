using System;
using System.IO;
using YouTubeVideoUploader.Application.Services;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Factories;

/// <summary>
/// Default implementation of IUploadJobFactory.
/// </summary>
public class UploadJobFactory : IUploadJobFactory
{
    private readonly TagGenerator _tagGenerator;

    /// <summary>
    /// Initializes a new instance of the UploadJobFactory.
    /// </summary>
    /// <param name="tagGenerator">The tag generator service.</param>
    public UploadJobFactory(TagGenerator tagGenerator)
    {
        _tagGenerator = tagGenerator ?? throw new ArgumentNullException(nameof(tagGenerator));
    }

    /// <inheritdoc />
    public UploadJob CreateJob(VideoFile file, UploadConfiguration config, DateTime? publishDate)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(config);

        // Title is the filename without extension (similar to Python script: os.path.splitext(file_name)[0])
        string title = Path.GetFileNameWithoutExtension(file.FileName);

        // Substitute placeholders in description
        string description = config.DescriptionTemplate.Replace("{GameName}", config.GameName, StringComparison.OrdinalIgnoreCase);

        // Generate dynamic + static tags
        var tags = _tagGenerator.GenerateTags(title, config.StaticTags);

        return new UploadJob(file, title)
        {
            Description = description,
            Tags = tags,
            CategoryId = config.CategoryId,
            PrivacyStatus = config.PrivacyStatus,
            ScheduledPublishDate = publishDate,
            PlaylistId = config.PlaylistId
        };
    }
}
