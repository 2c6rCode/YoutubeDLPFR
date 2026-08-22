using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using YtDlpGui.Models;
using YtDlpGui.Services;
using YtDlpGui.Views;

namespace YtDlpGui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;
    private readonly IYtDlpService _ytDlpService;
    private readonly IFfmpegService _ffmpegService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private BaseViewModel _currentViewModel;

    [ObservableProperty]
    private NavigationItem _selectedNavigationItem;

    [ObservableProperty]
    private string _ytDlpStatus = "Checking...";

    [ObservableProperty]
    private string _ffmpegStatus = "Checking...";

    [ObservableProperty]
    private string _appStatus = "Ready";

    [ObservableProperty]
    private bool _isSettingsOpen;

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IDownloadService downloadService,
        ISettingsService settingsService,
        IYtDlpService ytDlpService,
        IFfmpegService ffmpegService,
        IUpdateService updateService,
        DownloadsViewModel downloadsViewModel,
        QueueViewModel queueViewModel,
        HistoryViewModel historyViewModel,
        SettingsViewModel settingsViewModel)
    {
        _logger = logger;
        _downloadService = downloadService;
        _settingsService = settingsService;
        _ytDlpService = ytDlpService;
        _ffmpegService = ffmpegService;
        _updateService = updateService;

        NavigationItems.Add(new NavigationItem("Downloads", "Downloads", downloadsViewModel, "\uE7C3"));
        NavigationItems.Add(new NavigationItem("Queue", "Queue", queueViewModel, "\uE72A"));
        NavigationItems.Add(new NavigationItem("History", "History", historyViewModel, "\uE81C"));
        NavigationItems.Add(new NavigationItem("Settings", "Settings", settingsViewModel, "\uE713"));

        SelectedNavigationItem = NavigationItems[0];
        CurrentViewModel = downloadsViewModel;

        SafeInitializeAsync();
    }

    private async void SafeInitializeAsync()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MainViewModel");
            AppStatus = "Initialization Error";
        }
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem value)
    {
        if (value != null)
        {
            CurrentViewModel = value.ViewModel;
            IsSettingsOpen = value.Name == "Settings";
        }
    }

    private async Task InitializeAsync()
    {
        await CheckDependenciesAsync();
        await _settingsService.LoadAsync();
    }

    private async Task CheckDependenciesAsync()
    {
        try
        {
            var ytDlpAvailable = await _ytDlpService.IsAvailableAsync();
            var ytDlpVersion = ytDlpAvailable ? await _ytDlpService.GetVersionAsync() : "Not found";
            YtDlpStatus = ytDlpAvailable ? $"yt-dlp {ytDlpVersion} ✓" : "yt-dlp: Not found ✗";

            var ffmpegAvailable = await _ffmpegService.IsAvailableAsync();
            var ffmpegVersion = ffmpegAvailable ? await _ffmpegService.GetVersionAsync() : "Not found";
            FfmpegStatus = ffmpegAvailable ? $"FFmpeg {ffmpegVersion.Split('\n')[0]} ✓" : "FFmpeg: Not found ✗";

            AppStatus = ytDlpAvailable && ffmpegAvailable ? "Ready" : "Dependencies missing";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check dependencies");
            YtDlpStatus = "yt-dlp: Error";
            FfmpegStatus = "FFmpeg: Error";
            AppStatus = "Error checking dependencies";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsItem = NavigationItems.FirstOrDefault(n => n.Name == "Settings");
        if (settingsItem != null)
        {
            SelectedNavigationItem = settingsItem;
        }
    }

    [RelayCommand]
    private async Task RefreshDependenciesAsync()
    {
        AppStatus = "Checking dependencies...";
        await CheckDependenciesAsync();
    }
}

public class NavigationItem
{
    public string Name { get; }
    public string DisplayName { get; }
    public BaseViewModel ViewModel { get; }
    public string IconGlyph { get; }

    public NavigationItem(string name, string displayName, BaseViewModel viewModel, string iconGlyph)
    {
        Name = name;
        DisplayName = displayName;
        ViewModel = viewModel;
        IconGlyph = iconGlyph;
    }
}