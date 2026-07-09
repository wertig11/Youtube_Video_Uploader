using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace YouTubeVideoUploader.Application.Services;

/// <summary>
/// Service that generates tags for YouTube videos based on static tags and dynamic tags extracted from the video title.
/// </summary>
public class TagGenerator
{
    private static readonly Regex CleanRegex = new(
        @"Master levels for doom 2|coop|#|doom", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex WordRegex = new(
        @"[a-zA-Z0-9]+", 
        RegexOptions.Compiled
    );

    /// <summary>
    /// Generates deduplicated list of tags for a video title.
    /// </summary>
    /// <param name="title">The video title.</param>
    /// <param name="staticTags">The static configuration tags.</param>
    /// <returns>A read-only list of generated tags.</returns>
    public IReadOnlyList<string> GenerateTags(string title, IReadOnlyList<string> staticTags)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return staticTags ?? Array.Empty<string>();
        }

        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add all static tags
        if (staticTags != null)
        {
            foreach (var tag in staticTags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(tag.Trim());
                }
            }
        }

        // Generate dynamic tags from title
        // 1. Clean the title by removing junk words
        string clean = CleanRegex.Replace(title, string.Empty);

        // 2. Extract level name (part after '-')
        string levelName;
        if (clean.Contains('-'))
        {
            levelName = clean.Split('-', 2)[1].Trim();
        }
        else
        {
            levelName = clean.Trim();
        }

        if (!string.IsNullOrEmpty(levelName))
        {
            // Add full level name as a tag (lowercased)
            tags.Add(levelName.ToLowerInvariant());

            // Extract individual words of length > 2
            var words = WordRegex.Matches(levelName)
                .Select(m => m.Value.ToLowerInvariant())
                .Where(w => w.Length > 2);

            foreach (var word in words)
            {
                tags.Add(word);
            }
        }

        return tags.ToList();
    }
}
