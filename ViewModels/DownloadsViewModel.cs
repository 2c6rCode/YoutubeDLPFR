using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using YtDlpGui.Models;
using YtDlpGui.Services;

namespace YtDlpGui.ViewModels;

public partial class DownloadsViewModel : BaseViewModel
{
    private readonly ILogger<DownloadsViewModel> _logger;
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    private bool _isUrlValid = false;

    [ObservableProperty]
    private ObservableCollection<DownloadItem> _queueItems = new();

    [ObservableProperty]
    private DownloadItem? _selectedItem;

    [ObservableProperty]
    private int _queuedCount = 0;

    [ObservableProperty]
    private int _downloadingCount = 0;

    [ObservableProperty]
    private int _completedCount = 0;

    public DownloadsViewModel(
        ILogger<DownloadsViewModel> logger,
        IDownloadService downloadService,
        ISettingsService settingsService)
    {
        _logger = logger;
        _downloadService = downloadService;
        _settingsService = settingsService;

        _downloadService.DownloadAdded += OnDownloadAdded;
        _downloadService.DownloadUpdated += OnDownloadUpdated;
        _downloadService.DownloadRemoved += OnDownloadRemoved;

        RefreshQueue();
    }

    partial void OnUrlInputChanged(string value)
    {
        IsUrlValid = IsValidUrl(value);
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    [RelayCommand(CanExecute = nameof(CanAddToQueue))]
    private async Task AddToQueueAsync()
    {
        if (!IsUrlValid || string.IsNullOrWhiteSpace(UrlInput))
            return;

        var urls = UrlInput.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var url in urls)
        {
            if (IsValidUrl(url))
            {
                await _downloadService.AddDownloadAsync(url.Trim());
            }
        }

        UrlInput = string.Empty;
        RefreshQueue();
    }

    private bool CanAddToQueue() => IsUrlValid && !string.IsNullOrWhiteSpace(UrlInput);

    [RelayCommand(CanExecute = nameof(CanStartDownloads))]
    private async Task StartDownloadsAsync()
    {
        var queuedItems = _downloadService.QueuedDownloads.ToList();
        
        foreach (var item in queuedItems)
        {
            await _downloadService.StartDownloadAsync(item.Id);
        }

        RefreshQueue();
    }

    private bool CanStartDownloads() => _downloadService.QueuedDownloads.Any();

    [RelayCommand]
    private async Task ClearQueueAsync()
    {
        await _downloadService.ClearAllAsync();
        RefreshQueue();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem != null)
        {
            _ = _downloadService.RemoveDownloadAsync(SelectedItem.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task RetrySelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanRetry)
        {
            await _downloadService.RetryDownloadAsync(SelectedItem.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task PauseSelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanPause)
        {
            await _downloadService.PauseDownloadAsync(SelectedItem.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task CancelSelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanCancel)
        {
            await _downloadService.CancelDownloadAsync(SelectedItem.Id);
            RefreshQueue();
        }
    }

    private void OnDownloadAdded(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            QueueItems.Add(item);
            UpdateCounts();
        });
    }

    private void OnDownloadUpdated(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            UpdateCounts();
        });
    }

    private void OnDownloadRemoved(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            QueueItems.Remove(item);
            UpdateCounts();
        });
    }

    private void RefreshQueue()
    {
        QueueItems.Clear();
        foreach (var item in _downloadService.AllDownloads)
        {
            QueueItems.Add(item);
        }
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        QueuedCount = _downloadService.QueuedDownloads.Count;
        DownloadingCount = _downloadService.ActiveDownloads.Count;
        CompletedCount = _downloadService.CompletedDownloads.Count;
    }
}