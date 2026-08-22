using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YtDlpGui.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var invert = parameter?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            var invert = parameter?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            return (visibility == Visibility.Visible) ^ invert;
        }
        return false;
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        var isNull = value == null;
        return (isNull ^ invert) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class DownloadStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.DownloadStatus status)
        {
            return status switch
            {
                Models.DownloadStatus.Pending => "#6B7280",
                Models.DownloadStatus.Queued => "#3B82F6",
                Models.DownloadStatus.Downloading => "#2563EB",
                Models.DownloadStatus.Merging => "#7C3AED",
                Models.DownloadStatus.PostProcessing => "#7C3AED",
                Models.DownloadStatus.Paused => "#F59E0B",
                Models.DownloadStatus.Completed => "#10B981",
                Models.DownloadStatus.Failed => "#EF4444",
                Models.DownloadStatus.Cancelled => "#9CA3AF",
                _ => "#6B7280"
            };
        }
        return "#6B7280";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class DownloadProgressToAngleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return (progress / 100.0) * 360.0;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return FormatBytes(bytes);
        }
        if (value is double dbytes)
        {
            return FormatBytes((long)dbytes);
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dblBytes = bytes;
        while (dblBytes >= 1024 && i < suffixes.Length - 1)
        {
            dblBytes /= 1024;
            i++;
        }
        return $"{dblBytes:F1} {suffixes[i]}";
    }
}

public class TimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return Format(timeSpan);
        }
        return "--:--";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string Format(TimeSpan timeSpan)
    {
        if (timeSpan <= TimeSpan.Zero)
            return "--:--";

        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        
        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
}

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue && parameter is string paramStr)
        {
            return enumValue.ToString() == paramStr;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string paramStr && targetType.IsEnum)
        {
            return Enum.Parse(targetType, paramStr);
        }
        return Binding.DoNothing;
    }
}

public class StringIsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        var isNullOrEmpty = string.IsNullOrWhiteSpace(value?.ToString());
        return isNullOrEmpty ^ invert;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}