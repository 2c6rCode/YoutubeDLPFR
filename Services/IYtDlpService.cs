using YtDlpGui.Models;

namespace YtDlpGui.Services;

public interface IYtDlpService
{
    event EventHandler<string>? OutputReceived;
    event EventHandler<string>? ErrorReceived;

    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<YtDlpVersionInfo?> GetLatestVersionInfoAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(CancellationToken cancellationToken = default, IProgress<double>? progress = null);
    Task<VideoInfo?> ExtractInfoAsync(string url, CancellationToken cancellationToken = default);
    Task<ProcessResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null);
    Task<ProcessResult> DownloadAsync(string url, DownloadOptions options, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null);
    string GetExecutablePath();
    void SetExecutablePath(string path);
}

public class VideoInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Uploader { get; set; } = string.Empty;
    public long Duration { get; set; }
    public string WebpageUrl { get; set; } = string.Empty;
    public List<FormatInfo> Formats { get; set; } = new();
    public List<SubtitleInfo> Subtitles { get; set; } = new();
    public bool IsPlaylist { get; set; }
    public List<VideoInfo> Entries { get; set; } = new();
}

public class FormatInfo
{
    public string FormatId { get; set; } = string.Empty;
    public string Ext { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string VCodec { get; set; } = string.Empty;
    public string ACodec { get; set; } = string.Empty;
    public long Filesize { get; set; }
    public double Fps { get; set; }
    public string FormatNote { get; set; } = string.Empty;
    public bool IsVideoOnly { get; set; }
    public bool IsAudioOnly { get; set; }
    public int Quality { get; set; }
}

public class SubtitleInfo
{
    public string Language { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string Ext { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success => ExitCode == 0;
    public TimeSpan ElapsedTime { get; set; }
}