using YtDlpGui.Models;

namespace YtDlpGui.Services;

public interface ISettingsService
{
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    AppSettings Settings { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);
    T GetSetting<T>(string key, T defaultValue = default!);
    Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}

public class SettingsChangedEventArgs : EventArgs
{
    public string Key { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}