using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using YtDlpGui.Models;

namespace YtDlpGui.Services;

public class FfmpegService : IFfmpegService
{
    private readonly ILogger<FfmpegService> _logger;
    private readonly ISettingsService _settingsService;
    private string _executablePath = "ffmpeg";

    public FfmpegService(ILogger<FfmpegService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;
        InitializePath();
    }

    private void InitializePath()
    {
        var settings = _settingsService.Settings;
        if (!string.IsNullOrWhiteSpace(settings.FfmpegPath) && File.Exists(settings.FfmpegPath))
        {
            _executablePath = settings.FfmpegPath;
        }
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Key == nameof(AppSettings.FfmpegPath))
        {
            InitializePath();
        }
    }

    public string GetExecutablePath() => _executablePath;

    public void SetExecutablePath(string path)
    {
        _executablePath = path;
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync("-version", cancellationToken);
        if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            var lines = result.StandardOutput.Split('\n');
            return lines.FirstOrDefault()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync("-version", cancellationToken);
        return result.Success;
    }

    public async Task<FfmpegInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var version = await GetVersionAsync(cancellationToken);
        var isAvailable = await IsAvailableAsync(cancellationToken);

        return new FfmpegInfo
        {
            Version = version,
            Path = _executablePath,
            IsAvailable = isAvailable,
            SupportedCodecs = Array.Empty<string>()
        };
    }

    public async Task<bool> AutoDetectAsync(CancellationToken cancellationToken = default)
    {
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Gyan", "ffmpeg", "bin", "ffmpeg.exe")
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                _executablePath = path;
                await _settingsService.SetSettingAsync(nameof(AppSettings.FfmpegPath), path, cancellationToken);
                return true;
            }
        }

        try
        {
            var result = await ExecuteAsync("-version", cancellationToken);
            if (result.Success)
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public async Task<ProcessResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString(),
                StandardError = errorBuilder.ToString(),
                ElapsedTime = DateTime.Now - startTime
            };
        }
        catch (OperationCanceledException)
        {
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = "Operation cancelled",
                ElapsedTime = DateTime.Now - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute ffmpeg with args: {Args}", arguments);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message,
                ElapsedTime = DateTime.Now - startTime
            };
        }
    }
}