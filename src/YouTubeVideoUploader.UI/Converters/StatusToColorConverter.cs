using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using YouTubeVideoUploader.Domain.Enums;

namespace YouTubeVideoUploader.UI.Converters;

/// <summary>
/// Converts an UploadStatus enum value to a corresponding brush color.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UploadStatus status)
        {
            return status switch
            {
                UploadStatus.Pending => new SolidColorBrush(Color.FromRgb(156, 163, 175)),    // Slate Gray (#9CA3AF)
                UploadStatus.Uploading => new SolidColorBrush(Color.FromRgb(59, 130, 246)),  // Blue (#3B82F6)
                UploadStatus.Completed => new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // Green (#22C55E)
                UploadStatus.Failed => new SolidColorBrush(Color.FromRgb(239, 68, 68)),      // Red (#EF4444)
                UploadStatus.Skipped => new SolidColorBrush(Color.FromRgb(245, 158, 11)),     // Amber/Yellow (#F59E0B)
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
