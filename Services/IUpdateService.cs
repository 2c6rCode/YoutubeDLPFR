using YtDlpGui.Models;

namespace YtDlpGui.Services;

public interface IUpdateService
{
    event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;

    Task<YtDlpUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateYtDlpAsync(CancellationToken cancellationToken = default, IProgress<double>? progress = null);
    Task<bool> UpdateFfmpegAsync(CancellationToken cancellationToken = default, IProgress<double>? progress = null);
    string GetCurrentYtDlpVersion();
    string GetCurrentFfmpegVersion();
}

public class UpdateCheckEventArgs : EventArgs
{
    public YtDlpUpdateInfo UpdateInfo { get; set; } = new();
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}