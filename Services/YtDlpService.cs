using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using YtDlpGui.Models;

namespace YtDlpGui.Services;

public class YtDlpService : IYtDlpService
{
    private readonly ILogger<YtDlpService> _logger;
    private readonly ISettingsService _settingsService;
    private string _executablePath = "yt-dlp";

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;

    public YtDlpService(ILogger<YtDlpService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _settingsService.SettingsChanged += OnSettingsChanged;
        InitializePath();
    }

    private void InitializePath()
    {
        var settings = _settingsService.Settings;
        if (!string.IsNullOrWhiteSpace(settings.YtDlpPath) && File.Exists(settings.YtDlpPath))
        {
            _executablePath = settings.YtDlpPath;
        }
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Key == nameof(AppSettings.YtDlpPath))
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
        var result = await ExecuteAsync("--version", cancellationToken);
        return result.Success ? result.StandardOutput.Trim() : string.Empty;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync("--version", cancellationToken);
        return result.Success;
    }

    public async Task<YtDlpVersionInfo?> GetLatestVersionInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("YtDlpGui/1.0");
            var response = await httpClient.GetAsync("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var version = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? string.Empty;
            var assets = root.GetProperty("assets").EnumerateArray()
                .Select(a => a.GetProperty("browser_download_url").GetString() ?? string.Empty)
                .Where(u => !string.IsNullOrEmpty(u))
                .ToArray();

            return new YtDlpVersionInfo
            {
                Version = version,
                ReleaseDate = root.GetProperty("published_at").GetString() ?? string.Empty,
                DownloadUrl = assets.FirstOrDefault(u => u.Contains("yt-dlp.exe")) ?? string.Empty,
                ChangelogUrl = root.GetProperty("html_url").GetString() ?? string.Empty,
                Assets = assets
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest yt-dlp version info");
            return null;
        }
    }

    public async Task<bool> UpdateAsync(CancellationToken cancellationToken = default, IProgress<double>? progress = null)
    {
        try
        {
            var versionInfo = await GetLatestVersionInfoAsync(cancellationToken);
            if (versionInfo == null || string.IsNullOrEmpty(versionInfo.DownloadUrl))
                return false;

            progress?.Report(0.1);

            var downloadPath = Path.Combine(Path.GetTempPath(), "yt-dlp_new.exe");
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("YtDlpGui/1.0");
            
            using var response = await httpClient.GetAsync(versionInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(downloadPath);

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;
                
                if (totalBytes > 0)
                {
                    progress?.Report(0.1 + 0.8 * (double)downloadedBytes / totalBytes);
                }
            }

            progress?.Report(0.9);

            var currentPath = _executablePath;
            if (File.Exists(currentPath))
            {
                File.Replace(downloadPath, currentPath, null);
            }
            else
            {
                File.Move(downloadPath, currentPath);
            }

            progress?.Report(1.0);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update yt-dlp");
            return false;
        }
    }

    public async Task<VideoInfo?> ExtractInfoAsync(string url, CancellationToken cancellationToken = default)
    {
        var args = $"--dump-json --no-download \"{url}\"";
        var result = await ExecuteAsync(args, cancellationToken);
        
        if (!result.Success || string.IsNullOrWhiteSpace(result.StandardOutput))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<VideoInfo>(result.StandardOutput, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse video info for {Url}", url);
            return null;
        }
    }

    public async Task<ProcessResult> ExecuteAsync(string arguments, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null)
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
            
            // Ensure process is killed if cancelled
            using var ctr = cancellationToken.Register(() => 
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
            });

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    OutputReceived?.Invoke(this, e.Data);
                    
                    if (progress != null && e.Data.StartsWith("PROGRESS:"))
                    {
                        ParseProgress(e.Data, progress);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    ErrorReceived?.Invoke(this, e.Data);
                }
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
            _logger.LogError(ex, "Failed to execute yt-dlp with args: {Args}", arguments);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message,
                ElapsedTime = DateTime.Now - startTime
            };
        }
    }

    private void ParseProgress(string data, IProgress<DownloadProgressEventArgs> progress)
    {
        try
        {
            // Format: PROGRESS:downloaded_bytes|total_bytes|speed|eta
            var parts = data.Substring(9).Split('|');
            if (parts.Length >= 4)
            {
                var args = new DownloadProgressEventArgs
                {
                    Status = DownloadStatus.Downloading
                };

                if (long.TryParse(parts[0], out var downloaded))
                    args.DownloadedBytes = downloaded;

                if (long.TryParse(parts[1], out var total))
                    args.TotalBytes = total;
                else if (long.TryParse(parts[1].Replace("~", ""), out var estTotal)) // Sometimes it's ~1000
                    args.TotalBytes = estTotal;

                if (double.TryParse(parts[2], out var speed))
                    args.SpeedBytesPerSecond = speed;

                if (int.TryParse(parts[3], out var etaSeconds))
                    args.Eta = TimeSpan.FromSeconds(etaSeconds);

                if (args.TotalBytes > 0)
                    args.Progress = (double)args.DownloadedBytes / args.TotalBytes * 100.0;

                progress.Report(args);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse progress data: {Data}", data);
        }
    }

    public async Task<ProcessResult> DownloadAsync(string url, DownloadOptions options, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null)
    {
        var args = BuildDownloadArguments(url, options);
        return await ExecuteAsync(args, cancellationToken, progress);
    }

    private string BuildDownloadArguments(string url, DownloadOptions options)
    {
        var args = new List<string>();

        if (options.Type == DownloadType.Audio)
        {
            args.Add("-x");
            args.Add("--audio-format");
            args.Add(options.Format ?? "mp3");
            args.Add("--audio-quality");
            args.Add(options.AudioQuality ?? "0");
        }
        else
        {
            var format = options.Format ?? "bestvideo+bestaudio/best";
            if (!string.IsNullOrEmpty(options.Quality) && options.Quality != "best")
            {
                format = $"bestvideo[height<={options.Quality}]+bestaudio/best[height<={options.Quality}]";
            }
            args.Add("-f");
            args.Add(format);
            
            if (options.KeepVideo)
                args.Add("--keep-video");
        }

        if (!string.IsNullOrEmpty(options.OutputPath))
        {
            args.Add("-o");
            args.Add(Path.Combine(options.OutputPath, "%(title)s.%(ext)s"));
        }

        if (options.DownloadSubtitles)
        {
            args.Add("--write-subs");
            args.Add("--write-auto-subs");
            args.Add("--sub-lang");
            args.Add("en,en-US,en-GB");
            
            if (options.EmbedSubtitles)
                args.Add("--embed-subs");
        }

        if (options.EmbedThumbnail)
            args.Add("--embed-thumbnail");

        if (options.EmbedMetadata)
            args.Add("--embed-metadata");

        if (options.NoOverwrites)
            args.Add("--no-overwrites");

        if (options.ContinueIncomplete)
            args.Add("--continue");

        if (!string.IsNullOrEmpty(options.Proxy))
        {
            args.Add("--proxy");
            args.Add(options.Proxy);
        }

        if (options.SocketTimeout > 0)
        {
            args.Add("--socket-timeout");
            args.Add(options.SocketTimeout.ToString());
        }

        if (!string.IsNullOrEmpty(options.Container))
        {
            args.Add("--merge-output-format");
            args.Add(options.Container);
        }

        if (!string.IsNullOrEmpty(options.PlaylistSelection))
        {
            args.Add("--playlist-items");
            args.Add(options.PlaylistSelection);
        }
        else if (options.Type != DownloadType.Playlist && options.Type != DownloadType.Channel)
        {
            args.Add("--no-playlist");
        }

        if (!string.IsNullOrEmpty(options.Cookies))
        {
            args.Add("--cookies");
            args.Add($"\"{options.Cookies}\"");
        }

        if (!string.IsNullOrEmpty(options.CustomArgs))
        {
            args.Add(options.CustomArgs);
        }

        args.Add("--newline");
        args.Add("--progress-template");
        args.Add("\"PROGRESS:%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.speed)s|%(progress.eta)s\"");
        args.Add("\"{url}\"");

        return string.Join(" ", args);
    }
}