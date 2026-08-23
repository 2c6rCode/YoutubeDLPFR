using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using YtDlpGui.Models;

namespace YtDlpGui.Services;

public class DownloadService : IDownloadService
{
    private readonly ILogger<DownloadService> _logger;
    private readonly IYtDlpService _ytDlpService;
    private readonly ISettingsService _settingsService;
    private readonly ConcurrentDictionary<string, DownloadItem> _downloads = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokenSources = new();
    private SemaphoreSlim _downloadSemaphore;
    private readonly object _lock = new();

    public event EventHandler<DownloadItem>? DownloadAdded;
    public event EventHandler<DownloadItem>? DownloadUpdated;
    public event EventHandler<DownloadItem>? DownloadRemoved;
    public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;

    public IReadOnlyList<DownloadItem> ActiveDownloads => _downloads.Values
        .Where(d => d.IsActive)
        .OrderByDescending(d => d.CreatedAt)
        .ToList();

    public IReadOnlyList<DownloadItem> QueuedDownloads => _downloads.Values
        .Where(d => d.Status == DownloadStatus.Queued || d.Status == DownloadStatus.Pending)
        .OrderBy(d => d.CreatedAt)
        .ToList();

    public IReadOnlyList<DownloadItem> CompletedDownloads => _downloads.Values
        .Where(d => d.IsFinished)
        .OrderByDescending(d => d.CompletedAt ?? d.CreatedAt)
        .ToList();

    public IReadOnlyList<DownloadItem> AllDownloads => _downloads.Values
        .OrderByDescending(d => d.CreatedAt)
        .ToList();

    public DownloadService(
        ILogger<DownloadService> logger,
        IYtDlpService ytDlpService,
        ISettingsService settingsService)
    {
        _logger = logger;
        _ytDlpService = ytDlpService;
        _settingsService = settingsService;
        _downloadSemaphore = new SemaphoreSlim(_settingsService.Settings.MaxConcurrentDownloads);
        
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Key == nameof(AppSettings.MaxConcurrentDownloads))
        {
            // Note: Dynamically changing SemaphoreSlim max count is complex.
            // For a robust implementation, a custom rate limiter is preferred.
            // Here we just create a new one for future tasks, but existing tasks
            // will continue using the old one (which is safe as long as we don't dispose it).
            var oldSemaphore = _downloadSemaphore;
            _downloadSemaphore = new SemaphoreSlim(_settingsService.Settings.MaxConcurrentDownloads);
        }
    }

    public async Task<DownloadItem> AddDownloadAsync(string url, DownloadOptions? options = null, CancellationToken cancellationToken = default)
    {
        var item = new DownloadItem
        {
            Url = url,
            Title = url,
            Status = DownloadStatus.Queued,
            Type = options?.Type ?? DownloadType.Video,
            Format = options?.Format ?? _settingsService.Settings.PreferredFormat,
            Quality = options?.Quality ?? _settingsService.Settings.PreferredQuality,
            CustomArgs = options?.CustomArgs ?? string.Empty,
            OutputPath = !string.IsNullOrWhiteSpace(options?.OutputPath) ? options.OutputPath : _settingsService.Settings.DefaultDownloadPath
        };

        _downloads.TryAdd(item.Id, item);
        DownloadAdded?.Invoke(this, item);
        DownloadUpdated?.Invoke(this, item);

        _logger.LogInformation("Added download {Id} for URL: {Url}", item.Id, url);

        _ = Task.Run(async () =>
        {
            try
            {
                var info = await _ytDlpService.ExtractInfoAsync(url, cancellationToken);
                if (info != null)
                {
                    if (!string.IsNullOrWhiteSpace(info.Title))
                        item.Title = info.Title;
                    if (!string.IsNullOrWhiteSpace(info.ThumbnailUrl))
                        item.ThumbnailUrl = info.ThumbnailUrl;

                    DownloadUpdated?.Invoke(this, item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract metadata for {Url}", url);
            }
        }, cancellationToken);

        return item;
    }

    public async Task<bool> StartDownloadAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        if (!_downloads.TryGetValue(downloadId, out var item))
            return false;

        if (item.Status != DownloadStatus.Queued && item.Status != DownloadStatus.Pending && item.Status != DownloadStatus.Paused && item.Status != DownloadStatus.Failed && item.Status != DownloadStatus.Cancelled)
            return false;

        item.Status = DownloadStatus.Queued;
        DownloadUpdated?.Invoke(this, item);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokenSources[downloadId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await _downloadSemaphore.WaitAsync(cts.Token);
                try
                {
                    await ProcessDownloadAsync(item, cts.Token);
                }
                finally
                {
                    _downloadSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                if (item.Status != DownloadStatus.Paused)
                {
                    item.Status = DownloadStatus.Cancelled;
                }
                DownloadUpdated?.Invoke(this, item);
            }
            finally
            {
                _cancellationTokenSources.TryRemove(downloadId, out _);
                cts.Dispose();
            }
        }, cts.Token);

        return true;
    }

    private async Task ProcessDownloadAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        try
        {
            item.Status = DownloadStatus.Downloading;
            item.StartedAt = DateTime.Now;
            DownloadUpdated?.Invoke(this, item);

            // Item-level CustomArgs (e.g. specific format id) takes priority; append global custom args after
            var mergedCustomArgs = string.IsNullOrWhiteSpace(item.CustomArgs)
                ? _settingsService.Settings.CustomArgs
                : string.IsNullOrWhiteSpace(_settingsService.Settings.CustomArgs)
                    ? item.CustomArgs
                    : $"{item.CustomArgs} {_settingsService.Settings.CustomArgs}";

            var options = new DownloadOptions
            {
                Type = item.Type,
                Format = item.Format,
                Quality = item.Quality,
                OutputPath = item.OutputPath,
                DownloadSubtitles = _settingsService.Settings.DownloadSubtitles,
                EmbedSubtitles = _settingsService.Settings.EmbedSubtitles,
                EmbedThumbnail = _settingsService.Settings.EmbedThumbnail,
                EmbedMetadata = _settingsService.Settings.EmbedMetadata,
                CustomArgs = mergedCustomArgs,
                AudioQuality = _settingsService.Settings.AudioQuality,
                KeepVideo = _settingsService.Settings.KeepVideo,
                NoOverwrites = false, // always allow re-downloading the same video
                ContinueIncomplete = item.Status == DownloadStatus.Paused && _settingsService.Settings.ContinueIncomplete,
                SocketTimeout = _settingsService.Settings.SocketTimeout,
                Proxy = _settingsService.Settings.Proxy,
                Container = _settingsService.Settings.Container,
                Cookies = _settingsService.Settings.Cookies
            };

            var progress = new Progress<DownloadProgressEventArgs>(args =>
            {
                args.DownloadId = item.Id;
                UpdateProgress(item, args);
            });

            var result = await _ytDlpService.DownloadAsync(item.Url, options, cancellationToken, progress);

            if (cancellationToken.IsCancellationRequested)
            {
                if (item.Status != DownloadStatus.Paused)
                {
                    item.Status = DownloadStatus.Cancelled;
                }
            }
            else if (result.Success)
            {
                item.Status = DownloadStatus.Completed;
                item.Progress = 100;
                item.DownloadedBytes = item.TotalBytes > 0 ? item.TotalBytes : item.DownloadedBytes;
                item.CompletedAt = DateTime.Now;
            }
            else
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = string.IsNullOrWhiteSpace(result.StandardError) ? "Download process failed." : result.StandardError;
                item.RetryCount++;
            }

            DownloadUpdated?.Invoke(this, item);
        }
        catch (OperationCanceledException)
        {
            if (item.Status != DownloadStatus.Paused)
            {
                item.Status = DownloadStatus.Cancelled;
            }
            DownloadUpdated?.Invoke(this, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download {Id} failed with exception", item.Id);
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = ex.Message;
            item.RetryCount++;
            DownloadUpdated?.Invoke(this, item);
        }
    }

    private void UpdateProgress(DownloadItem item, DownloadProgressEventArgs args)
    {
        if (args.Progress > 0)
            item.Progress = args.Progress;

        if (args.DownloadedBytes > 0)
            item.DownloadedBytes = args.DownloadedBytes;

        if (args.TotalBytes > 0)
            item.TotalBytes = args.TotalBytes;

        if (args.SpeedBytesPerSecond > 0)
            item.SpeedBytesPerSecond = args.SpeedBytesPerSecond;

        if (args.Eta > TimeSpan.Zero)
            item.Eta = args.Eta;
        
        if (args.Status != DownloadStatus.Downloading && args.Status != DownloadStatus.Pending && args.Status != DownloadStatus.Queued)
            item.Status = args.Status;

        if (!string.IsNullOrEmpty(args.ErrorMessage))
            item.ErrorMessage = args.ErrorMessage;

        ProgressChanged?.Invoke(this, args);
        DownloadUpdated?.Invoke(this, item);
    }

    public async Task<bool> PauseDownloadAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        if (!_downloads.TryGetValue(downloadId, out var item))
            return false;

        if (item.Status != DownloadStatus.Downloading)
            return false;

        item.Status = DownloadStatus.Paused;

        if (_cancellationTokenSources.TryGetValue(downloadId, out var cts))
        {
            try { cts.Cancel(); } catch { }
        }

        DownloadUpdated?.Invoke(this, item);
        return true;
    }

    public async Task<bool> ResumeDownloadAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        return await StartDownloadAsync(downloadId, cancellationToken);
    }

    public async Task<bool> CancelDownloadAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        if (!_downloads.TryGetValue(downloadId, out var item))
            return false;

        if (!item.CanCancel)
            return false;

        item.Status = DownloadStatus.Cancelled;

        if (_cancellationTokenSources.TryGetValue(downloadId, out var cts))
        {
            try { cts.Cancel(); } catch { }
        }

        DownloadUpdated?.Invoke(this, item);
        return true;
    }

    public async Task<bool> RetryDownloadAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        if (!_downloads.TryGetValue(downloadId, out var item))
            return false;

        if (!item.CanRetry)
            return false;

        item.Status = DownloadStatus.Queued;
        item.ErrorMessage = string.Empty;
        item.Progress = 0;
        item.DownloadedBytes = 0;
        item.SpeedBytesPerSecond = 0;
        item.Eta = TimeSpan.Zero;
        DownloadUpdated?.Invoke(this, item);

        return await StartDownloadAsync(downloadId, cancellationToken);
    }

    public async Task<bool> RemoveDownloadAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSources.TryGetValue(downloadId, out var cts))
        {
            try { cts.Cancel(); } catch { }
        }

        if (!_downloads.TryRemove(downloadId, out var item))
            return false;

        DownloadRemoved?.Invoke(this, item);
        return true;
    }

    public async Task ClearCompletedAsync(CancellationToken cancellationToken = default)
    {
        var completed = _downloads.Values.Where(d => d.IsFinished).ToList();
        foreach (var item in completed)
        {
            _downloads.TryRemove(item.Id, out _);
            DownloadRemoved?.Invoke(this, item);
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var kvp in _cancellationTokenSources)
        {
            try { kvp.Value.Cancel(); } catch { }
        }

        var all = _downloads.Values.ToList();
        foreach (var item in all)
        {
            _downloads.TryRemove(item.Id, out _);
            DownloadRemoved?.Invoke(this, item);
        }
    }
}