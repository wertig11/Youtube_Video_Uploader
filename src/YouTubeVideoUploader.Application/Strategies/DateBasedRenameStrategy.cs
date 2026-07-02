using System;
using System.Text.RegularExpressions;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;

namespace YouTubeVideoUploader.Application.Strategies;

/// <summary>
/// A renaming strategy that renames files based on their creation date using custom formatting templates.
/// </summary>
public class DateBasedRenameStrategy : IRenameStrategy
{
    /// <inheritdoc />
    public string DisplayName => "Rename by creation date";

    /// <inheritdoc />
    public bool RequiresNameList => false;

    /// <inheritdoc />
    public string GenerateNewName(VideoFile file, int index, RenameTemplate template)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(template);

        string pattern = template.Pattern;

        // Replace {Date} or {Date:format} using regex
        pattern = Regex.Replace(pattern, @"\{Date(?::([^}]+))?\}", match =>
        {
            string formatString = "yyyy-MM-dd"; // Default format
            if (match.Groups[1].Success)
            {
                formatString = match.Groups[1].Value;
            }
            return file.CreationDate.ToString(formatString);
        }, RegexOptions.IgnoreCase);

        // Replace {Number} with index+1
        pattern = Regex.Replace(pattern, @"\{Number\}", (index + 1).ToString(), RegexOptions.IgnoreCase);

        return $"{pattern}{file.Extension}";
    }
}
