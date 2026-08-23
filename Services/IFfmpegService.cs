
using YtDlpGui.Models;

namespace YtDlpGui.Services;

public interface IFfmpegService
{
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<FfmpegInfo> GetInfoAsync(CancellationToken cancellationToken = default);
    Task<bool> AutoDetectAsync(CancellationToken cancellationToken = default);
    string GetExecutablePath();
    void SetExecutablePath(string path);
    Task<ProcessResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default);
}