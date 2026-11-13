using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using YtDlpGui.Models;
using YtDlpGui.Services;

namespace YtDlpGui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IYtDlpService _ytDlpService;
    private readonly IFfmpegService _ffmpegService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private AppSettings _settings;

    [ObservableProperty]
    private string _ytDlpVersion = string.Empty;

    [ObservableProperty]
    private string _ffmpegVersion = string.Empty;

    [ObservableProperty]
    private bool _isCheckingUpdates = false;

    [ObservableProperty]
    private string _updateStatus = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableBrowserCookies = new()
    {
        "none", "chrome", "edge", "firefox", "brave", "opera", "vivaldi"
    };

    [RelayCommand]
    private void BrowsePluginDirs()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select yt-dlp Plugins Directory"
        };
        if (dialog.ShowDialog() == true)
        {
            Settings.PluginDirs = dialog.FolderName;
        }
    }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ISettingsService settingsService,
        IYtDlpService ytDlpService,
        IFfmpegService ffmpegService,
        IUpdateService updateService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _ytDlpService = ytDlpService;
        _ffmpegService = ffmpegService;
        _updateService = updateService;

        Settings = _settingsService.Settings;
        
        _settingsService.SettingsChanged += OnSettingsChanged;
        
        _ = LoadVersionsAsync();
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        if (e.Key == nameof(AppSettings.YtDlpPath))
        {
            _ = LoadYtDlpVersionAsync();
        }
        else if (e.Key == nameof(AppSettings.FfmpegPath))
        {
            _ = LoadFfmpegVersionAsync();
        }
    }

    private async Task LoadVersionsAsync()
    {
        await LoadYtDlpVersionAsync();
        await LoadFfmpegVersionAsync();
    }

    private async Task LoadYtDlpVersionAsync()
    {
        try
        {
            YtDlpVersion = await _ytDlpService.GetVersionAsync() ?? "Not found";
        }
        catch
        {
            YtDlpVersion = "Error";
        }
    }

    private async Task LoadFfmpegVersionAsync()
    {
        try
        {
            FfmpegVersion = await _ffmpegService.GetVersionAsync() ?? "Not found";
        }
        catch
        {
            FfmpegVersion = "Error";
        }
    }

    [RelayCommand]
    private void BrowseYtDlpPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select yt-dlp executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = "yt-dlp.exe"
        };

        if (dialog.ShowDialog() == true)
        {
            Settings.YtDlpPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseFfmpegPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select FFmpeg executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = "ffmpeg.exe"
        };

        if (dialog.ShowDialog() == true)
        {
            Settings.FfmpegPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseDownloadPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select default download folder"
        };

        if (dialog.ShowDialog() == true)
        {
            Settings.DefaultDownloadPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task UpdateYtDlpSelfAsync()
    {
        IsCheckingUpdates = true;
        UpdateStatus = "Updating yt-dlp (running yt-dlp -U)...";

        try
        {
            var result = await _ytDlpService.UpdateSelfAsync();
            var output = !string.IsNullOrWhiteSpace(result.StandardOutput) 
                ? result.StandardOutput.Trim() 
                : (!string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardError.Trim() : "Update completed.");
            
            UpdateStatus = output;
            await LoadYtDlpVersionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update yt-dlp");
            UpdateStatus = $"Update error: {ex.Message}";
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    private async Task AutoDetectYtDlpAsync()
    {
        IsBusy = true;
        try
        {
            var found = await _ytDlpService.IsAvailableAsync();
            if (found)
            {
                var version = await _ytDlpService.GetVersionAsync();
                UpdateStatus = $"yt-dlp {version} found in PATH";
            }
            else
            {
                UpdateStatus = "yt-dlp not found in PATH";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-detect yt-dlp");
            UpdateStatus = "Error detecting yt-dlp";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AutoDetectFfmpegAsync()
    {
        IsBusy = true;
        try
        {
            var found = await _ffmpegService.AutoDetectAsync();
            if (found)
            {
                var version = await _ffmpegService.GetVersionAsync();
                UpdateStatus = $"FFmpeg {version.Split('\n')[0]} found";
            }
            else
            {
                UpdateStatus = "FFmpeg not found";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-detect FFmpeg");
            UpdateStatus = "Error detecting FFmpeg";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdates = true;
        UpdateStatus = "Checking for updates...";
        
        try
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync();
            
            if (updateInfo.UpdateAvailable)
            {
                UpdateStatus = $"Update available: {updateInfo.LatestVersion} (current: {updateInfo.CurrentVersion})";
            }
            else
            {
                UpdateStatus = $"Up to date (v{updateInfo.CurrentVersion})";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            UpdateStatus = "Error checking for updates";
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    private async Task UpdateYtDlpAsync()
    {
        IsBusy = true;
        UpdateStatus = "Updating yt-dlp...";
        
        try
        {
            var progress = new Progress<double>(p => 
            {
                UpdateStatus = $"Updating yt-dlp... {p:P0}";
            });

            var success = await _updateService.UpdateYtDlpAsync(CancellationToken.None, progress);
            
            if (success)
            {
                await LoadYtDlpVersionAsync();
                UpdateStatus = $"Updated to yt-dlp {YtDlpVersion}";
            }
            else
            {
                UpdateStatus = "Failed to update yt-dlp";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update yt-dlp");
            UpdateStatus = "Error updating yt-dlp";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsBusy = true;
        try
        {
            await _settingsService.SaveAsync();
            UpdateStatus = "Settings saved";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            UpdateStatus = "Error saving settings";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        IsBusy = true;
        try
        {
            await _settingsService.ResetToDefaultsAsync();
            Settings = _settingsService.Settings;
            UpdateStatus = "Settings reset to defaults";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings");
            UpdateStatus = "Error resetting settings";
        }
        finally
        {
            IsBusy = false;
        }
    }
}