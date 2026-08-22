using Microsoft.Extensions.Logging;
using YtDlpGui.Models;

namespace YtDlpGui.Services;

public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IYtDlpService _ytDlpService;
    private readonly IFfmpegService _ffmpegService;

    public event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;

    public UpdateService(
        ILogger<UpdateService> logger,
        IYtDlpService ytDlpService,
        IFfmpegService ffmpegService)
    {
        _logger = logger;
        _ytDlpService = ytDlpService;
        _ffmpegService = ffmpegService;
    }

    public async Task<YtDlpUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = await _ytDlpService.GetVersionAsync(cancellationToken);
        var latestInfo = await _ytDlpService.GetLatestVersionInfoAsync(cancellationToken);

        var updateInfo = new YtDlpUpdateInfo
        {
            CurrentVersion = currentVersion,
            CheckedAt = DateTime.UtcNow
        };

        if (latestInfo != null)
        {
            updateInfo.LatestVersion = latestInfo.Version;
            updateInfo.UpdateAvailable = IsNewerVersion(latestInfo.Version, currentVersion);
            
            if (updateInfo.UpdateAvailable)
            {
                updateInfo.ReleaseNotes = $"New version {latestInfo.Version} available (current: {currentVersion})";
            }
        }

        UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs { UpdateInfo = updateInfo });
        return updateInfo;
    }

    public async Task<bool> UpdateYtDlpAsync(CancellationToken cancellationToken = default, IProgress<double>? progress = null)
    {
        return await _ytDlpService.UpdateAsync(cancellationToken, progress);
    }

    public async Task<bool> UpdateFfmpegAsync(CancellationToken cancellationToken = default, IProgress<double>? progress = null)
    {
        _logger.LogWarning("FFmpeg auto-update not implemented yet");
        return false;
    }

    public string GetCurrentYtDlpVersion()
    {
        return _ytDlpService.GetExecutablePath();
    }

    public string GetCurrentFfmpegVersion()
    {
        return _ffmpegService.GetExecutablePath();
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (string.IsNullOrEmpty(latest) || string.IsNullOrEmpty(current))
            return false;

        try
        {
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
            {
                var latestPart = i < latestParts.Length ? latestParts[i] : 0;
                var currentPart = i < currentParts.Length ? currentParts[i] : 0;

                if (latestPart > currentPart) return true;
                if (latestPart < currentPart) return false;
            }
        }
        catch
        {
            return string.Compare(latest, current, StringComparison.OrdinalIgnoreCase) > 0;
        }

        return false;
    }
}