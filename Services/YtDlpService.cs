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
            return;
        }

        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var localExe = Path.Combine(appDir, "yt-dlp.exe");
        if (File.Exists(localExe))
        {
            _executablePath = localExe;
            return;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathDirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in pathDirs)
        {
            try
            {
                var candidate = Path.Combine(dir, "yt-dlp.exe");
                if (File.Exists(candidate))
                {
                    _executablePath = candidate;
                    return;
                }
            }
            catch { }
        }

        _executablePath = "yt-dlp";
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

    public async Task<ProcessResult> UpdateSelfAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync("-U", cancellationToken);
    }

    public async Task<MediaAnalysisResult> AnalyzeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new MediaAnalysisResult
            {
                Success = false,
                ErrorMessage = "URL cannot be empty."
            };
        }

        var args = new List<string>
        {
            "--dump-json",
            "--no-playlist",
            "--extractor-args",
            "youtube:player_client=web,default",
            url
        };

        var result = await ExecuteAsync(args, cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            var err = CleanYtDlpErrorMessage(result.StandardError);
            return new MediaAnalysisResult
            {
                Success = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(err) ? err : "Failed to retrieve media information."
            };
        }

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            var videoInfo = JsonSerializer.Deserialize<VideoInfo>(result.StandardOutput, jsonOptions);

            // Fallback manual JsonDocument parse if Deserializer missed essential fields
            if (videoInfo != null && string.IsNullOrEmpty(videoInfo.Title))
            {
                using var doc = JsonDocument.Parse(result.StandardOutput);
                var root = doc.RootElement;

                videoInfo.Title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                videoInfo.Uploader = root.TryGetProperty("uploader", out var upProp) ? upProp.GetString() ?? "" : "";
                videoInfo.Channel = root.TryGetProperty("channel", out var chProp) ? chProp.GetString() ?? "" : "";
                videoInfo.ThumbnailUrl = root.TryGetProperty("thumbnail", out var thumbProp) ? thumbProp.GetString() ?? "" : "";
                if (root.TryGetProperty("duration", out var durProp) && durProp.TryGetDouble(out var durVal))
                    videoInfo.DurationSeconds = durVal;
                if (root.TryGetProperty("view_count", out var vcProp) && vcProp.TryGetDouble(out var vcVal))
                    videoInfo.ViewCount = vcVal;
                if (root.TryGetProperty("upload_date", out var udProp))
                    videoInfo.UploadDateRaw = udProp.GetString();
            }

            if (videoInfo == null)
            {
                return new MediaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse metadata."
                };
            }

            return new MediaAnalysisResult
            {
                Success = true,
                Info = videoInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse video info for {Url}", url);

            // Resilient emergency JsonDocument extraction
            try
            {
                using var doc = JsonDocument.Parse(result.StandardOutput);
                var root = doc.RootElement;
                var fallbackInfo = new VideoInfo
                {
                    Title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Unknown Title" : "Unknown Title",
                    Uploader = root.TryGetProperty("uploader", out var upProp) ? upProp.GetString() ?? "" : "",
                    Channel = root.TryGetProperty("channel", out var chProp) ? chProp.GetString() ?? "" : "",
                    ThumbnailUrl = root.TryGetProperty("thumbnail", out var thumbProp) ? thumbProp.GetString() ?? "" : "",
                    UploadDateRaw = root.TryGetProperty("upload_date", out var udProp) ? udProp.GetString() : null
                };
                if (root.TryGetProperty("duration", out var durProp) && durProp.TryGetDouble(out var durVal))
                    fallbackInfo.DurationSeconds = durVal;
                if (root.TryGetProperty("view_count", out var vcProp) && vcProp.TryGetDouble(out var vcVal))
                    fallbackInfo.ViewCount = vcVal;

                return new MediaAnalysisResult
                {
                    Success = true,
                    Info = fallbackInfo
                };
            }
            catch
            {
                return new MediaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Received invalid metadata format from yt-dlp."
                };
            }
        }
    }

    private static string CleanYtDlpErrorMessage(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
            return "An unknown error occurred while analyzing the media.";

        var lines = stderr.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var relevantErrors = lines
            .Where(l => l.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Substring(6).Trim())
            .ToList();

        if (relevantErrors.Count > 0)
        {
            var joined = string.Join("\n", relevantErrors);
            if (joined.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase))
                return "Unsupported URL or website.";
            if (joined.Contains("Private video", StringComparison.OrdinalIgnoreCase))
                return "This video is private.";
            if (joined.Contains("Sign in to confirm", StringComparison.OrdinalIgnoreCase) || joined.Contains("requires authentication", StringComparison.OrdinalIgnoreCase))
                return "This video requires authentication or login.";
            if (joined.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase) || joined.Contains("deleted", StringComparison.OrdinalIgnoreCase))
                return "This video is unavailable or has been deleted.";
            if (joined.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase))
                return "Access forbidden (HTTP 403). The site blocked the request.";
            if (joined.Contains("Unable to download webpage", StringComparison.OrdinalIgnoreCase) || joined.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase) || joined.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
                return "Network failure: Unable to reach the host.";

            return joined;
        }

        return lines.LastOrDefault() ?? "Failed to inspect media.";
    }

    public async Task<VideoInfo?> ExtractInfoAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = await AnalyzeUrlAsync(url, cancellationToken);
        return result.Info;
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

            return await RunProcessInternalAsync(startInfo, startTime, cancellationToken, progress);
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

    public async Task<ProcessResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            return await RunProcessInternalAsync(startInfo, startTime, cancellationToken, progress);
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
            _logger.LogError(ex, "Failed to execute yt-dlp with argument list");
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message,
                ElapsedTime = DateTime.Now - startTime
            };
        }
    }

    private async Task<ProcessResult> RunProcessInternalAsync(ProcessStartInfo startInfo, DateTime startTime, CancellationToken cancellationToken, IProgress<DownloadProgressEventArgs>? progress)
    {
        using var process = new Process { StartInfo = startInfo };
        
        using var ctr = cancellationToken.Register(() => 
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
        });

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.Start();

        var outputTask = ReadStreamAsync(process.StandardOutput, outputBuilder, data => 
        {
            OutputReceived?.Invoke(this, data);
            if (progress != null)
            {
                ParseOutputLine(data, progress);
            }
        }, cancellationToken);

        var errorTask = ReadStreamAsync(process.StandardError, errorBuilder, data => 
        {
            ErrorReceived?.Invoke(this, data);
        }, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(outputTask, errorTask);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            ElapsedTime = DateTime.Now - startTime
        };
    }

    private async Task ReadStreamAsync(StreamReader reader, StringBuilder builder, Action<string> onLineRead, CancellationToken cancellationToken)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                builder.AppendLine(line);
                onLineRead(line);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }

    private void ParseOutputLine(string data, IProgress<DownloadProgressEventArgs> progress)
    {
        try
        {
            if (data.StartsWith("PROGRESS:"))
            {
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
                    else if (long.TryParse(parts[1].Replace("~", ""), out var estTotal))
                        args.TotalBytes = estTotal;

                    if (double.TryParse(parts[2], out var speed))
                        args.SpeedBytesPerSecond = speed;

                    if (int.TryParse(parts[3], out var etaSeconds))
                        args.Eta = TimeSpan.FromSeconds(etaSeconds);

                    if (args.TotalBytes > 0)
                        args.Progress = (double)args.DownloadedBytes / args.TotalBytes * 100.0;

                    progress.Report(args);
                }
                return;
            }

            if (data.Contains("[Merger]") || data.Contains("Merging formats"))
            {
                progress.Report(new DownloadProgressEventArgs { Status = DownloadStatus.Merging });
                return;
            }

            if (data.Contains("[ExtractAudio]") || data.Contains("[Fixup") || data.Contains("[EmbedThumbnail]"))
            {
                progress.Report(new DownloadProgressEventArgs { Status = DownloadStatus.PostProcessing });
                return;
            }

            if (data.StartsWith("[download]") && data.Contains("%"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    data,
                    @"\[download\]\s+(\d+(?:\.\d+)?)%");

                if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var pct))
                {
                    progress.Report(new DownloadProgressEventArgs
                    {
                        Status = DownloadStatus.Downloading,
                        Progress = pct
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse progress data: {Data}", data);
        }
    }

    public async Task<ProcessResult> DownloadAsync(string url, DownloadOptions options, CancellationToken cancellationToken = default, IProgress<DownloadProgressEventArgs>? progress = null)
    {
        var args = BuildDownloadArgumentList(url, options);
        return await ExecuteAsync(args, cancellationToken, progress);
    }

    private List<string> BuildDownloadArgumentList(string url, DownloadOptions options)
    {
        var args = new List<string>();

        // skip format/quality args when CustomArgs already contains -f (e.g. from Inspect panel)
        bool customArgsHasFormat = !string.IsNullOrEmpty(options.CustomArgs) &&
            System.Text.RegularExpressions.Regex.IsMatch(options.CustomArgs, @"(^|\s)-f(\s|$)");

        if (options.Type == DownloadType.Audio)
        {
            args.Add("-x");
            args.Add("--audio-format");
            args.Add(options.Format ?? "mp3");
            args.Add("--audio-quality");
            args.Add(options.AudioQuality ?? "0");
        }
        else if (!customArgsHasFormat)
        {
            var format = options.Format ?? "bestvideo+bestaudio/best";
            bool isMp4 = options.Format == "mp4";

            if (!string.IsNullOrEmpty(options.Quality) && options.Quality != "best")
            {
                if (isMp4)
                    format = $"bestvideo[ext=mp4][height<={options.Quality}]+bestaudio[ext=m4a]/best[ext=mp4][height<={options.Quality}]/best";
                else
                    format = $"bestvideo[height<={options.Quality}]+bestaudio/best[height<={options.Quality}]";
            }
            else if (isMp4)
            {
                format = "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best";
            }
            args.Add("-f");
            args.Add(format);

            if (options.KeepVideo)
                args.Add("--keep-video");
        }

        if (!string.IsNullOrEmpty(options.OutputPath))
        {
            args.Add("-o");
            var template = !string.IsNullOrWhiteSpace(options.OutputTemplate)
                ? options.OutputTemplate
                : "%(title)s.%(ext)s";
            
            var qualitySuffix = (!string.IsNullOrEmpty(options.Quality) && options.Quality != "best")
                ? $".{options.Quality}p"
                : string.Empty;

            if (!string.IsNullOrEmpty(qualitySuffix) && template.Contains(".%(ext)s"))
                template = template.Replace(".%(ext)s", $"{qualitySuffix}.%(ext)s");

            args.Add(Path.Combine(options.OutputPath, template));
        }

        if (!string.IsNullOrEmpty(options.DownloadSections))
        {
            args.Add("--download-sections");
            args.Add(options.DownloadSections);
            args.Add("--force-keyframes-at-cuts");
        }

        if (options.SponsorBlock)
        {
            args.Add("--sponsorblock-remove");
            args.Add("all");
        }

        if (options.GeoBypass)
        {
            args.Add("--geo-bypass");
            if (!string.IsNullOrEmpty(options.GeoBypassCountry))
            {
                args.Add("--geo-bypass-country");
                args.Add(options.GeoBypassCountry);
            }
        }

        if (options.UseNetrc)
        {
            args.Add("--netrc");
        }
        else if (!string.IsNullOrEmpty(options.Username))
        {
            args.Add("--username");
            args.Add(options.Username);
            if (!string.IsNullOrEmpty(options.Password))
            {
                args.Add("--password");
                args.Add(options.Password);
            }
            if (!string.IsNullOrEmpty(options.TwoFactor))
            {
                args.Add("--twofactor");
                args.Add(options.TwoFactor);
            }
        }

        if (!string.IsNullOrEmpty(options.PluginDirs))
        {
            args.Add("--plugin-dirs");
            args.Add(options.PluginDirs);
        }

        // always overwrite so re-downloading at a different quality replaces the old file
        args.Add("--force-overwrites");

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

        if (!string.IsNullOrEmpty(options.CookiesFromBrowser) && options.CookiesFromBrowser != "none")
        {
            args.Add("--cookies-from-browser");
            args.Add(options.CookiesFromBrowser);
        }
        else if (!string.IsNullOrEmpty(options.Cookies))
        {
            args.Add("--cookies");
            args.Add(options.Cookies);
        }

        if (!string.IsNullOrEmpty(options.CustomArgs))
        {
            // Simple split by space, but respect quotes
            var parts = System.Text.RegularExpressions.Regex.Matches(options.CustomArgs, @"[""].+?[""]|[^ ]+")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value.Trim('"'))
                .ToList();
            foreach (var part in parts)
            {
                args.Add(part);
            }
        }

        // Add default extractor args to bypass PO-Token if not already provided
        if (string.IsNullOrEmpty(options.CustomArgs) || !options.CustomArgs.Contains("--extractor-args"))
        {
            args.Add("--extractor-args");
            args.Add("youtube:player_client=web,default");
        }

        args.Add("--newline");
        args.Add("--progress-template");
        args.Add("PROGRESS:%(progress.downloaded_bytes)s|%(progress.total_bytes,progress.total_bytes_estimate)s|%(progress.speed)s|%(progress.eta)s");
        args.Add(url);

        return args;
    }
}