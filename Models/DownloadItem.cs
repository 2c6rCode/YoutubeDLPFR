using CommunityToolkit.Mvvm.ComponentModel;
using System;
using YtDlpGui.Converters;

namespace YtDlpGui.Models;

public enum DownloadStatus
{
    Pending,
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
    Cancelled,
    Merging,
    PostProcessing
}

public enum DownloadType
{
    Video,
    Audio,
    Playlist,
    Channel
}

public partial class DownloadItem : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _thumbnailUrl = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(IsFinished))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    [NotifyPropertyChangedFor(nameof(CanResume))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    private DownloadStatus _status = DownloadStatus.Pending;

    [ObservableProperty]
    private DownloadType _type = DownloadType.Video;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedProgress))]
    private double _progress = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotal))]
    [NotifyPropertyChangedFor(nameof(FormattedSize))]
    private long _totalBytes = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedDownloaded))]
    [NotifyPropertyChangedFor(nameof(FormattedSize))]
    private long _downloadedBytes = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSpeed))]
    private double _speedBytesPerSecond = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedEta))]
    private TimeSpan _eta = TimeSpan.Zero;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _outputFilename = string.Empty;

    [ObservableProperty]
    private string _format = string.Empty;

    [ObservableProperty]
    private string _quality = string.Empty;

    [ObservableProperty]
    private string _customArgs = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime? _startedAt;

    [ObservableProperty]
    private DateTime? _completedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    private int _retryCount = 0;

    [ObservableProperty]
    private bool _isSelected = false;

    public string FormattedSpeed => FormatBytes(SpeedBytesPerSecond) + "/s";
    public string FormattedProgress => $"{Progress:F1}%";
    public string FormattedDownloaded => FormatBytes(DownloadedBytes);
    public string FormattedTotal => FormatBytes(TotalBytes);
    public string FormattedSize => TotalBytes > 0 ? $"{FormatBytes(DownloadedBytes)} / {FormatBytes(TotalBytes)}" : (DownloadedBytes > 0 ? FormatBytes(DownloadedBytes) : "0 B");
    public string FormattedEta => TimeSpanConverter.Format(Eta);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsActive => Status is DownloadStatus.Downloading or DownloadStatus.Merging or DownloadStatus.PostProcessing;
    public bool IsFinished => Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled;
    public bool CanStart => Status is DownloadStatus.Queued or DownloadStatus.Pending;
    public bool CanRetry => Status is DownloadStatus.Failed or DownloadStatus.Cancelled;
    public bool CanPause => Status == DownloadStatus.Downloading;
    public bool CanResume => Status == DownloadStatus.Paused;
    public bool CanCancel => Status is DownloadStatus.Queued or DownloadStatus.Downloading or DownloadStatus.Paused;

    private static string FormatBytes(double bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        while (bytes >= 1024 && i < suffixes.Length - 1)
        {
            bytes /= 1024;
            i++;
        }
        return $"{bytes:F1} {suffixes[i]}";
    }
}