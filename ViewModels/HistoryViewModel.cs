using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using YtDlpGui.Models;
using YtDlpGui.Services;

namespace YtDlpGui.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly ILogger<HistoryViewModel> _logger;
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private ObservableCollection<DownloadItem> _historyItems = new();

    [ObservableProperty]
    private DownloadItem? _selectedItem;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private DownloadStatus? _filterStatus;

    [ObservableProperty]
    private int _totalCompleted = 0;

    [ObservableProperty]
    private int _totalFailed = 0;

    [ObservableProperty]
    private long _totalDownloadedBytes = 0;

    public HistoryViewModel(
        ILogger<HistoryViewModel> logger,
        IDownloadService downloadService)
    {
        _logger = logger;
        _downloadService = downloadService;

        _downloadService.DownloadAdded += OnDownloadChanged;
        _downloadService.DownloadUpdated += OnDownloadChanged;
        _downloadService.DownloadRemoved += OnDownloadChanged;

        RefreshHistory();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnFilterStatusChanged(DownloadStatus? value)
    {
        ApplyFilters();
    }

    private void OnDownloadChanged(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (item.IsFinished)
            {
                RefreshHistory();
            }
        });
    }

    private void RefreshHistory()
    {
        var items = _downloadService.CompletedDownloads.ToList();
        
        HistoryItems.Clear();
        foreach (var item in items)
        {
            HistoryItems.Add(item);
        }

        UpdateStats();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        // Filtering is handled in the view via CollectionViewSource
    }

    private void UpdateStats()
    {
        TotalCompleted = HistoryItems.Count(i => i.Status == DownloadStatus.Completed);
        TotalFailed = HistoryItems.Count(i => i.Status == DownloadStatus.Failed);
        TotalDownloadedBytes = HistoryItems.Where(i => i.Status == DownloadStatus.Completed).Sum(i => i.DownloadedBytes);
    }

    [RelayCommand]
    private async Task RetrySelectedAsync()
    {
        if (SelectedItem != null && SelectedItem.CanRetry)
        {
            await _downloadService.RetryDownloadAsync(SelectedItem.Id);
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
    private async Task ClearHistoryAsync()
    {
        await _downloadService.ClearCompletedAsync();
    }

    [RelayCommand]
    private void OpenFileLocation()
    {
        if (SelectedItem != null && SelectedItem.Status == DownloadStatus.Completed)
        {
            try
            {
                var directory = Path.GetDirectoryName(SelectedItem.OutputPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open file location");
            }
        }
    }
}