using YtDlpGui.Models;

namespace YtDlpGui.Services;

public interface IDownloadService
{
    event EventHandler<DownloadItem>? DownloadAdded;
    event EventHandler<DownloadItem>? DownloadUpdated;
    event EventHandler<DownloadItem>? DownloadRemoved;
    event EventHandler<DownloadProgressEventArgs>? ProgressChanged;

    IReadOnlyList<DownloadItem> ActiveDownloads { get; }
    IReadOnlyList<DownloadItem> QueuedDownloads { get; }
    IReadOnlyList<DownloadItem> CompletedDownloads { get; }
    IReadOnlyList<DownloadItem> AllDownloads { get; }

    Task<DownloadItem> AddDownloadAsync(string url, DownloadOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> StartDownloadAsync(string downloadId, CancellationToken cancellationToken = default);
    Task<bool> PauseDownloadAsync(string downloadId, CancellationToken cancellationToken = default);
    Task<bool> ResumeDownloadAsync(string downloadId, CancellationToken cancellationToken = default);
    Task<bool> CancelDownloadAsync(string downloadId, CancellationToken cancellationToken = default);
    Task<bool> RetryDownloadAsync(string downloadId, CancellationToken cancellationToken = default);
    Task<bool> RemoveDownloadAsync(string downloadId, CancellationToken cancellationToken = default);
    Task ClearCompletedAsync(CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}

public class DownloadProgressEventArgs : EventArgs
{
    public string DownloadId { get; set; } = string.Empty;
    public double Progress { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double SpeedBytesPerSecond { get; set; }
    public TimeSpan Eta { get; set; }
    public DownloadStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DownloadOptions
{
    public DownloadType Type { get; set; } = DownloadType.Video;
    public string? Format { get; set; }
    public string? Quality { get; set; }
    public string? OutputPath { get; set; }
    public bool DownloadSubtitles { get; set; }
    public bool EmbedSubtitles { get; set; }
    public bool EmbedThumbnail { get; set; }
    public bool EmbedMetadata { get; set; }
    public string? CustomArgs { get; set; }
    public string? AudioQuality { get; set; }
    public string? Container { get; set; }
    public string? PlaylistSelection { get; set; }
    public string? Cookies { get; set; }
    public bool KeepVideo { get; set; }
    public bool NoOverwrites { get; set; }
    public bool ContinueIncomplete { get; set; }
    public string? Proxy { get; set; }
    public int SocketTimeout { get; set; } = 30;
}