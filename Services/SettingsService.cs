using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using YtDlpGui.Models;

namespace YtDlpGui.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;
    private AppSettings _settings = AppSettings.Default;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public AppSettings Settings => _settings;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtDlpGui");
        
        Directory.CreateDirectory(appDataPath);
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loadedSettings != null)
                {
                    _settings = loadedSettings;
                    EnsureDefaultDownloadPath();
                }
            }
            else
            {
                EnsureDefaultDownloadPath();
                await SaveAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _settings = AppSettings.Default;
            EnsureDefaultDownloadPath();
        }
    }

    private void EnsureDefaultDownloadPath()
    {
        if (string.IsNullOrWhiteSpace(_settings.DefaultDownloadPath))
        {
            _settings.DefaultDownloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "YtDlpGui");
        }

        Directory.CreateDirectory(_settings.DefaultDownloadPath);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var oldSettings = _settings;
        _settings = AppSettings.Default;
        EnsureDefaultDownloadPath();
        await SaveAsync(cancellationToken);

        foreach (var prop in typeof(AppSettings).GetProperties())
        {
            var oldValue = prop.GetValue(oldSettings);
            var newValue = prop.GetValue(_settings);
            if (!Equals(oldValue, newValue))
            {
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
                {
                    Key = prop.Name,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }
    }

    public T GetSetting<T>(string key, T defaultValue = default!)
    {
        var prop = typeof(AppSettings).GetProperty(key);
        if (prop != null)
        {
            var value = prop.GetValue(_settings);
            if (value is T typedValue)
                return typedValue;
        }
        return defaultValue;
    }

    public async Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var prop = typeof(AppSettings).GetProperty(key);
        if (prop != null && prop.CanWrite)
        {
            var oldValue = prop.GetValue(_settings);
            prop.SetValue(_settings, value);
            await SaveAsync(cancellationToken);

            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = value
            });
        }
    }
}