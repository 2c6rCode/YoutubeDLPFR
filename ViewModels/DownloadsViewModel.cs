using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using YtDlpGui.Models;
using YtDlpGui.Services;

namespace YtDlpGui.ViewModels;

public partial class DownloadsViewModel : BaseViewModel
{
    private readonly ILogger<DownloadsViewModel> _logger;
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;
    private readonly IYtDlpService _ytDlpService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private bool _isUrlValid = false;

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private string? _analysisErrorMessage;

    [ObservableProperty]
    private VideoInfo? _inspectedMedia;

    [ObservableProperty]
    private FormatInfo? _selectedVideoFormat;

    [ObservableProperty]
    private FormatInfo? _selectedAudioFormat;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private DownloadType _selectedDownloadType = DownloadType.Video;

    [ObservableProperty]
    private string _selectedFormat = "mp4";

    [ObservableProperty]
    private string _selectedQuality = "best";

    [ObservableProperty]
    private ObservableCollection<string> _availableFormats = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableQualities = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDownloads))]
    [NotifyPropertyChangedFor(nameof(ActiveDownloads))]
    private ObservableCollection<DownloadItem> _queueItems = new();

    public ObservableCollection<DownloadItem> ActiveDownloads => QueueItems;

    public bool HasDownloads => QueueItems.Count > 0;

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
        ISettingsService settingsService,
        IYtDlpService ytDlpService)
    {
        _logger = logger;
        _downloadService = downloadService;
        _settingsService = settingsService;
        _ytDlpService = ytDlpService;

        _outputPath = !string.IsNullOrWhiteSpace(_settingsService.Settings.DefaultDownloadPath)
            ? _settingsService.Settings.DefaultDownloadPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        UpdateFormatAndQualityLists();

        _downloadService.DownloadAdded += OnDownloadAdded;
        _downloadService.DownloadUpdated += OnDownloadUpdated;
        _downloadService.DownloadRemoved += OnDownloadRemoved;

        RefreshQueue();
    }

    partial void OnUrlInputChanged(string value)
    {
        IsUrlValid = IsValidUrl(value);
    }

    partial void OnSelectedDownloadTypeChanged(DownloadType value)
    {
        UpdateFormatAndQualityLists();
    }

    private void UpdateFormatAndQualityLists()
    {
        AvailableFormats.Clear();
        AvailableQualities.Clear();

        if (SelectedDownloadType == DownloadType.Audio)
        {
            AvailableFormats.Add("mp3");
            AvailableFormats.Add("m4a");
            AvailableFormats.Add("opus");
            AvailableFormats.Add("wav");
            AvailableFormats.Add("flac");
            SelectedFormat = "mp3";

            AvailableQualities.Add("0 (Best)");
            AvailableQualities.Add("2 (High)");
            AvailableQualities.Add("5 (Medium)");
            AvailableQualities.Add("9 (Low)");
            SelectedQuality = "0 (Best)";
        }
        else
        {
            AvailableFormats.Add("mp4");
            AvailableFormats.Add("mkv");
            AvailableFormats.Add("webm");
            SelectedFormat = "mp4";

            AvailableQualities.Add("best");
            AvailableQualities.Add("2160");
            AvailableQualities.Add("1440");
            AvailableQualities.Add("1080");
            AvailableQualities.Add("720");
            AvailableQualities.Add("480");
            AvailableQualities.Add("360");
            SelectedQuality = "best";
        }
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return true; // Let yt-dlp handle validation
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Output Folder",
            InitialDirectory = Directory.Exists(OutputPath) ? OutputPath : string.Empty
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FolderName;
        }
    }

    private bool CanAddToQueue() => !IsAnalyzing && !string.IsNullOrWhiteSpace(UrlInput);
    private bool CanDownload() => !IsAnalyzing && !string.IsNullOrWhiteSpace(UrlInput);
    private bool CanAnalyze() => !IsAnalyzing && !string.IsNullOrWhiteSpace(UrlInput);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(UrlInput) || !IsValidUrl(UrlInput))
        {
            AnalysisErrorMessage = "Please enter a valid URL.";
            return;
        }

        IsAnalyzing = true;
        AnalysisErrorMessage = null;
        InspectedMedia = null;
        SelectedVideoFormat = null;
        SelectedAudioFormat = null;
        AddToQueueCommand.NotifyCanExecuteChanged();
        DownloadCommand.NotifyCanExecuteChanged();
        AnalyzeCommand.NotifyCanExecuteChanged();

        try
        {
            var result = await _ytDlpService.AnalyzeUrlAsync(UrlInput.Trim());
            if (result.Success && result.Info != null)
            {
                InspectedMedia = result.Info;
                SelectedVideoFormat = InspectedMedia.VideoFormats.FirstOrDefault();
                SelectedAudioFormat = InspectedMedia.AudioFormats.FirstOrDefault();
            }
            else
            {
                AnalysisErrorMessage = result.ErrorMessage ?? "Failed to analyze URL.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during media analysis");
            AnalysisErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
            AddToQueueCommand.NotifyCanExecuteChanged();
            DownloadCommand.NotifyCanExecuteChanged();
            AnalyzeCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void ClearInspection()
    {
        InspectedMedia = null;
        AnalysisErrorMessage = null;
        SelectedVideoFormat = null;
        SelectedAudioFormat = null;
    }

    [RelayCommand]
    private async Task DownloadCustomFormatAsync()
    {
        if (InspectedMedia == null) return;

        string? customFormatArg = null;
        string? formatExt = null;
        var downloadType = DownloadType.Video;

        if (SelectedVideoFormat != null && SelectedAudioFormat != null)
        {
            customFormatArg = $"{SelectedVideoFormat.FormatId}+{SelectedAudioFormat.FormatId}";
            formatExt = SelectedVideoFormat.Ext;
        }
        else if (SelectedVideoFormat != null)
        {
            customFormatArg = SelectedVideoFormat.FormatId;
            formatExt = SelectedVideoFormat.Ext;
        }
        else if (SelectedAudioFormat != null)
        {
            customFormatArg = SelectedAudioFormat.FormatId;
            formatExt = SelectedAudioFormat.Ext;
            downloadType = DownloadType.Audio;
        }

        var options = new DownloadOptions
        {
            Type = downloadType,
            Format = formatExt ?? SelectedFormat,
            CustomArgs = !string.IsNullOrEmpty(customFormatArg) ? $"-f {customFormatArg}" : null,
            OutputPath = OutputPath
        };

        var item = await _downloadService.AddDownloadAsync(UrlInput.Trim(), options);
        if (item != null)
        {
            await _downloadService.StartDownloadAsync(item.Id);
        }
    }

    private DownloadOptions CreateDownloadOptions()
    {
        var qualityClean = SelectedQuality.Contains(" ") ? SelectedQuality.Split(' ')[0] : SelectedQuality;
        return new DownloadOptions
        {
            Type = SelectedDownloadType,
            Format = SelectedFormat,
            Quality = qualityClean,
            AudioQuality = SelectedDownloadType == DownloadType.Audio ? qualityClean : null,
            OutputPath = OutputPath
        };
    }

    [RelayCommand(CanExecute = nameof(CanAddToQueue))]
    private async Task AddToQueueAsync()
    {
        if (string.IsNullOrWhiteSpace(UrlInput) || !IsValidUrl(UrlInput))
        {
            AnalysisErrorMessage = "Please enter a valid URL.";
            return;
        }

        var urls = UrlInput.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var options = CreateDownloadOptions();

        foreach (var url in urls)
        {
            if (IsValidUrl(url))
            {
                await _downloadService.AddDownloadAsync(url.Trim(), options);
            }
        }

        UrlInput = string.Empty;
        ClearInspection();
        RefreshQueue();
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(UrlInput) || !IsValidUrl(UrlInput))
        {
            AnalysisErrorMessage = "Please enter a valid URL.";
            return;
        }

        var urls = UrlInput.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var options = CreateDownloadOptions();

        foreach (var url in urls)
        {
            if (IsValidUrl(url))
            {
                var item = await _downloadService.AddDownloadAsync(url.Trim(), options);
                await _downloadService.StartDownloadAsync(item.Id);
            }
        }

        UrlInput = string.Empty;
        ClearInspection();
        RefreshQueue();
    }

    [RelayCommand]
    private async Task StartDownloadsAsync()
    {
        var queuedItems = _downloadService.QueuedDownloads.ToList();
        foreach (var item in queuedItems)
        {
            await _downloadService.StartDownloadAsync(item.Id);
        }
        RefreshQueue();
    }

    [RelayCommand]
    private async Task StartItemAsync(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        if (target != null)
        {
            await _downloadService.StartDownloadAsync(target.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task PauseAsync(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        if (target != null && target.CanPause)
        {
            await _downloadService.PauseDownloadAsync(target.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task ResumeAsync(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        if (target != null && target.CanResume)
        {
            await _downloadService.ResumeDownloadAsync(target.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task CancelAsync(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        if (target != null && target.CanCancel)
        {
            await _downloadService.CancelDownloadAsync(target.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task RetryAsync(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        if (target != null && target.CanRetry)
        {
            await _downloadService.RetryDownloadAsync(target.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        if (target != null)
        {
            await _downloadService.RemoveDownloadAsync(target.Id);
            RefreshQueue();
        }
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        await _downloadService.ClearCompletedAsync();
        RefreshQueue();
    }

    [RelayCommand]
    private async Task ClearQueueAsync()
    {
        await _downloadService.ClearAllAsync();
        RefreshQueue();
    }

    [RelayCommand]
    private void OpenFolder(DownloadItem? item)
    {
        var target = item ?? SelectedItem;
        var dir = target?.OutputPath;
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open folder {Dir}", dir);
            }
        }
    }

    private void OnDownloadAdded(object? sender, DownloadItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (!QueueItems.Any(d => d.Id == item.Id))
            {
                QueueItems.Add(item);
            }
            OnPropertyChanged(nameof(HasDownloads));
            OnPropertyChanged(nameof(ActiveDownloads));
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
            var found = QueueItems.FirstOrDefault(d => d.Id == item.Id);
            if (found != null)
            {
                QueueItems.Remove(found);
            }
            OnPropertyChanged(nameof(HasDownloads));
            OnPropertyChanged(nameof(ActiveDownloads));
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
        OnPropertyChanged(nameof(HasDownloads));
        OnPropertyChanged(nameof(ActiveDownloads));
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        QueuedCount = _downloadService.QueuedDownloads.Count;
        DownloadingCount = _downloadService.ActiveDownloads.Count;
        CompletedCount = _downloadService.CompletedDownloads.Count;
    }
}