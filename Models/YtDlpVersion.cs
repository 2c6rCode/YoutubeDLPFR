namespace YtDlpGui.Models;

public class YtDlpVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ChangelogUrl { get; set; } = string.Empty;
    public string[] Assets { get; set; } = Array.Empty<string>();
}

public class YtDlpUpdateInfo
{
    public bool UpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class FfmpegInfo
{
    public string Version { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string[] SupportedCodecs { get; set; } = Array.Empty<string>();
}