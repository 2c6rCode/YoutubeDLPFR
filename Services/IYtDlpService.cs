using System.Text.Json.Serialization;
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
    Task<ProcessResult> UpdateSelfAsync(CancellationToken cancellationToken = default);
    Task<MediaAnalysisResult> AnalyzeUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<VideoInfo?> ExtractInfoAsync(string url, CancellationToken cancellationToken = default);
    Task<ProcessResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null);
    Task<ProcessResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null);
    Task<ProcessResult> DownloadAsync(string url, DownloadOptions options, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null);
    string GetExecutablePath();
    void SetExecutablePath(string path);
}

public class MediaAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public VideoInfo? Info { get; set; }
}

public class VideoInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("thumbnail")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [JsonPropertyName("uploader")]
    public string Uploader { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    public string DisplayUploader => !string.IsNullOrEmpty(Channel) ? Channel : Uploader;

    [JsonPropertyName("duration")]
    public double? DurationSeconds { get; set; }

    public long Duration => (long)(DurationSeconds ?? 0);

    public TimeSpan DurationTimeSpan => TimeSpan.FromSeconds(DurationSeconds ?? 0);

    [JsonPropertyName("upload_date")]
    public string? UploadDateRaw { get; set; }

    public string FormattedUploadDate
    {
        get
        {
            if (string.IsNullOrWhiteSpace(UploadDateRaw)) return "Unknown";
            if (UploadDateRaw.Length == 8 &&
                int.TryParse(UploadDateRaw.AsSpan(0, 4), out var year) &&
                int.TryParse(UploadDateRaw.AsSpan(4, 2), out var month) &&
                int.TryParse(UploadDateRaw.AsSpan(6, 2), out var day))
            {
                try { return new DateTime(year, month, day).ToString("MMM dd, yyyy"); }
                catch { return UploadDateRaw; }
            }
            return UploadDateRaw;
        }
    }

    [JsonPropertyName("view_count")]
    public double? ViewCount { get; set; }

    public string FormattedViewCount
    {
        get
        {
            if (ViewCount == null) return "N/A";
            return ((long)ViewCount.Value).ToString("N0");
        }
    }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("webpage_url")]
    public string WebpageUrl { get; set; } = string.Empty;

    [JsonPropertyName("ext")]
    public string ContainerExt { get; set; } = string.Empty;

    [JsonPropertyName("formats")]
    public List<FormatInfo> Formats { get; set; } = new();

    public IEnumerable<FormatInfo> VideoFormats => Formats
        .Where(f => !f.IsStoryBoard && (f.VCodec != "none" && !string.IsNullOrEmpty(f.VCodec)))
        .OrderByDescending(f => f.Height ?? 0)
        .ThenByDescending(f => f.Fps ?? 0)
        .ThenByDescending(f => f.Tbr ?? 0);

    public IEnumerable<FormatInfo> AudioFormats => Formats
        .Where(f => !f.IsStoryBoard && (f.ACodec != "none" && !string.IsNullOrEmpty(f.ACodec)) && (f.VCodec == "none" || string.IsNullOrEmpty(f.VCodec)))
        .OrderByDescending(f => f.Abr ?? f.Tbr ?? 0);

    [JsonPropertyName("subtitles")]
    public Dictionary<string, List<SubtitleInfoJson>>? SubtitlesRaw { get; set; }

    [JsonPropertyName("_type")]
    public string? EntryType { get; set; }

    public bool IsPlaylist => EntryType == "playlist" || Entries.Count > 0;

    [JsonPropertyName("entries")]
    public List<VideoInfo> Entries { get; set; } = new();
}

public class SubtitleInfoJson
{
    [JsonPropertyName("ext")]
    public string Ext { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class FormatInfo
{
    [JsonPropertyName("format_id")]
    public string FormatId { get; set; } = string.Empty;

    [JsonPropertyName("ext")]
    public string Ext { get; set; } = string.Empty;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public double? Width { get; set; }

    [JsonPropertyName("height")]
    public double? Height { get; set; }

    public string FormattedResolution => !string.IsNullOrEmpty(Resolution) && Resolution != "audio only" 
        ? Resolution 
        : (Height.HasValue && Width.HasValue ? $"{Math.Round(Width.Value)}x{Math.Round(Height.Value)}" : (Height.HasValue ? $"{Math.Round(Height.Value)}p" : (IsAudioOnly ? "Audio Only" : "N/A")));

    [JsonPropertyName("vcodec")]
    public string VCodec { get; set; } = string.Empty;

    [JsonPropertyName("acodec")]
    public string ACodec { get; set; } = string.Empty;

    [JsonPropertyName("filesize")]
    public double? Filesize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public double? FilesizeApprox { get; set; }

    public long? EffectiveFilesize => Filesize.HasValue ? (long)Filesize.Value : (FilesizeApprox.HasValue ? (long)FilesizeApprox.Value : null);

    [JsonPropertyName("fps")]
    public double? Fps { get; set; }

    public string FormattedFps => Fps.HasValue && Fps.Value > 0 ? $"{Math.Round(Fps.Value)} fps" : "-";

    [JsonPropertyName("tbr")]
    public double? Tbr { get; set; }

    [JsonPropertyName("vbr")]
    public double? Vbr { get; set; }

    [JsonPropertyName("abr")]
    public double? Abr { get; set; }

    public string FormattedBitrate
    {
        get
        {
            var bitrate = Tbr ?? (Vbr.HasValue || Abr.HasValue ? (Vbr ?? 0) + (Abr ?? 0) : (double?)null);
            return bitrate.HasValue && bitrate.Value > 0 ? $"{Math.Round(bitrate.Value)} kbps" : "-";
        }
    }

    [JsonPropertyName("format_note")]
    public string FormatNote { get; set; } = string.Empty;

    [JsonPropertyName("container")]
    public string Container { get; set; } = string.Empty;

    public string EffectiveContainer => !string.IsNullOrEmpty(Container) ? Container : Ext;

    public bool IsStoryBoard => Ext == "mhtml" || FormatNote.Contains("storyboard", StringComparison.OrdinalIgnoreCase);

    public bool IsVideoOnly => (VCodec != "none" && !string.IsNullOrEmpty(VCodec)) && (ACodec == "none" || string.IsNullOrEmpty(ACodec));

    public bool IsAudioOnly => (ACodec != "none" && !string.IsNullOrEmpty(ACodec)) && (VCodec == "none" || string.IsNullOrEmpty(VCodec));

    [JsonPropertyName("quality")]
    public double? Quality { get; set; }
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