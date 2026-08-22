using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using YtDlpGui.Models;
using YtDlpGui.Services;

namespace YtDlpGui.ViewModels;

public partial class QueueViewModel : BaseViewModel
{
    private readonly ILogger<QueueViewModel> _logger;
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private ObservableCollection<DownloadItem> _activeDownloads = new();

    [ObservableProperty]
    private ObservableCollection<DownloadItem> _queuedDownloads = new();

    [ObservableProperty]
    private DownloadItem? _selectedItem;

    [ObservableProperty]
    private int _totalActive = 0;

    [ObservableProperty]
    private int _totalQueued = 0;

    public QueueViewModel(
        ILogger<QueueViewModel> logger,
        IDownloadService downloadService)
    {
        _logger = logger;
        _downloadService = downloadService;

        _downloadService.DownloadAdded += OnDownloadChanged;
        _downloadService.DownloadUpdated += OnDownloadChanged;
        _downloadService.DownloadRemoved += OnDownloadChanged;
        _downloadService.ProgressChanged += OnProgressChanged;

        RefreshQueue();
    }

    private void OnDownloadChanged(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(RefreshQueue);
    }

    private void OnProgressChanged(object? sender, DownloadProgressEventArgs args)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var item = ActiveDownloads.FirstOrDefault(d => d.Id == args.DownloadId)
                ?? QueuedDownloads.FirstOrDefault(d => d.Id == args.DownloadId);
            
            if (item != null)
            {
                item.Progress = args.Progress;
                item.DownloadedBytes = args.DownloadedBytes;
                item.TotalBytes = args.TotalBytes;
                item.SpeedBytesPerSecond = args.SpeedBytesPerSecond;
                item.Eta = args.Eta;
                item.Status = args.Status;
                
                if (!string.IsNullOrEmpty(args.ErrorMessage))
                    item.ErrorMessage = args.ErrorMessage;
            }
        });
    }

    private void RefreshQueue()
    {
        ActiveDownloads.Clear();
        foreach (var item in _downloadService.ActiveDownloads)
        {
            ActiveDownloads.Add(item);
        }

        QueuedDownloads.Clear();
        foreach (var item in _downloadService.QueuedDownloads)
        {
            QueuedDownloads.Add(item);
        }

        TotalActive = ActiveDownloads.Count;
        TotalQueued = QueuedDownloads.Count;
    }

    [RelayCommand]
    private async Task PauseSelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanPause)
        {
            await _downloadService.PauseDownloadAsync(SelectedItem.Id);
        }
    }

    [RelayCommand]
    private async Task ResumeSelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanResume)
        {
            await _downloadService.ResumeDownloadAsync(SelectedItem.Id);
        }
    }

    [RelayCommand]
    private async Task CancelSelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanCancel)
        {
            await _downloadService.CancelDownloadAsync(SelectedItem.Id);
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        if (SelectedItem != null)
        {
            await _downloadService.RemoveDownloadAsync(SelectedItem.Id);
        }
    }

    [RelayCommand]
    private async Task StartAllQueuedAsync()
    {
        var queued = _downloadService.QueuedDownloads.ToList();
        foreach (var item in queued)
        {
            await _downloadService.StartDownloadAsync(item.Id);
        }
    }

    [RelayCommand]
    private async Task PauseAllAsync()
    {
        var active = _downloadService.ActiveDownloads.ToList();
        foreach (var item in active)
        {
            await _downloadService.PauseDownloadAsync(item.Id);
        }
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        await _downloadService.ClearCompletedAsync();
    }
}