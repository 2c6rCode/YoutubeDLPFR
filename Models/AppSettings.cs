using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;

namespace YtDlpGui.Models;

public partial class AppSettings : ObservableObject
{
    private string _ytDlpPath = string.Empty;
    private string _ffmpegPath = string.Empty;
    private string _defaultDownloadPath = string.Empty;
    private string _preferredQuality = "best";
    private string _preferredFormat = "mp4";
    private bool _downloadSubtitles = false;
    private bool _embedSubtitles = false;
    private bool _embedThumbnail = false;
    private bool _embedMetadata = true;
    private int _maxConcurrentDownloads = 3;
    private int _maxRetries = 3;
    private bool _autoUpdateYtDlp = true;
    private bool _checkUpdatesOnStartup = true;
    private bool _minimizeToTray = false;
    private bool _closeToTray = false;
    private string _theme = "Dark";
    private string _language = "en";
    private bool _showNotifications = true;
    private bool _playSoundOnComplete = false;
    private string _customArgs = string.Empty;
    private string _audioQuality = "0";
    private bool _keepVideo = false;
    private bool _noOverwrites = false;
    private bool _continueIncomplete = true;
    private int _socketTimeout = 30;
    private string _proxy = string.Empty;
    private string _container = string.Empty;
    private string _cookies = string.Empty;
    private string _browserCookies = "none";
    private bool _sponsorBlockEnabled = false;
    private string _outputTemplate = "%(title)s.%(ext)s";
    private bool _geoBypass = false;
    private string _geoBypassCountry = string.Empty;
    private bool _useNetrc = false;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _twoFactor = string.Empty;
    private string _pluginDirs = string.Empty;

    public string YtDlpPath
    {
        get => _ytDlpPath;
        set => SetProperty(ref _ytDlpPath, value);
    }

    public string FfmpegPath
    {
        get => _ffmpegPath;
        set => SetProperty(ref _ffmpegPath, value);
    }

    public string DefaultDownloadPath
    {
        get => _defaultDownloadPath;
        set => SetProperty(ref _defaultDownloadPath, value);
    }

    public string PreferredQuality
    {
        get => _preferredQuality;
        set => SetProperty(ref _preferredQuality, value);
    }

    public string PreferredFormat
    {
        get => _preferredFormat;
        set => SetProperty(ref _preferredFormat, value);
    }

    public bool DownloadSubtitles
    {
        get => _downloadSubtitles;
        set => SetProperty(ref _downloadSubtitles, value);
    }

    public bool EmbedSubtitles
    {
        get => _embedSubtitles;
        set => SetProperty(ref _embedSubtitles, value);
    }

    public bool EmbedThumbnail
    {
        get => _embedThumbnail;
        set => SetProperty(ref _embedThumbnail, value);
    }

    public bool EmbedMetadata
    {
        get => _embedMetadata;
        set => SetProperty(ref _embedMetadata, value);
    }

    public int MaxConcurrentDownloads
    {
        get => _maxConcurrentDownloads;
        set => SetProperty(ref _maxConcurrentDownloads, Math.Clamp(value, 1, 10));
    }

    public int MaxRetries
    {
        get => _maxRetries;
        set => SetProperty(ref _maxRetries, Math.Clamp(value, 0, 10));
    }

    public bool AutoUpdateYtDlp
    {
        get => _autoUpdateYtDlp;
        set => SetProperty(ref _autoUpdateYtDlp, value);
    }

    public bool CheckUpdatesOnStartup
    {
        get => _checkUpdatesOnStartup;
        set => SetProperty(ref _checkUpdatesOnStartup, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set => SetProperty(ref _closeToTray, value);
    }

    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public bool ShowNotifications
    {
        get => _showNotifications;
        set => SetProperty(ref _showNotifications, value);
    }

    public bool PlaySoundOnComplete
    {
        get => _playSoundOnComplete;
        set => SetProperty(ref _playSoundOnComplete, value);
    }

    public string CustomArgs
    {
        get => _customArgs;
        set => SetProperty(ref _customArgs, value);
    }

    public string AudioQuality
    {
        get => _audioQuality;
        set => SetProperty(ref _audioQuality, value);
    }

    public bool KeepVideo
    {
        get => _keepVideo;
        set => SetProperty(ref _keepVideo, value);
    }

    public bool NoOverwrites
    {
        get => _noOverwrites;
        set => SetProperty(ref _noOverwrites, value);
    }

    public bool ContinueIncomplete
    {
        get => _continueIncomplete;
        set => SetProperty(ref _continueIncomplete, value);
    }

    public int SocketTimeout
    {
        get => _socketTimeout;
        set => SetProperty(ref _socketTimeout, Math.Clamp(value, 5, 300));
    }

    public string Proxy
    {
        get => _proxy;
        set => SetProperty(ref _proxy, value);
    }

    public string Container
    {
        get => _container;
        set => SetProperty(ref _container, value);
    }

    public string Cookies
    {
        get => _cookies;
        set => SetProperty(ref _cookies, value);
    }

    public string BrowserCookies
    {
        get => _browserCookies;
        set => SetProperty(ref _browserCookies, value);
    }

    public bool SponsorBlockEnabled
    {
        get => _sponsorBlockEnabled;
        set => SetProperty(ref _sponsorBlockEnabled, value);
    }

    public string OutputTemplate
    {
        get => _outputTemplate;
        set => SetProperty(ref _outputTemplate, value);
    }

    public bool GeoBypass
    {
        get => _geoBypass;
        set => SetProperty(ref _geoBypass, value);
    }

    public string GeoBypassCountry
    {
        get => _geoBypassCountry;
        set => SetProperty(ref _geoBypassCountry, value);
    }

    public bool UseNetrc
    {
        get => _useNetrc;
        set => SetProperty(ref _useNetrc, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string TwoFactor
    {
        get => _twoFactor;
        set => SetProperty(ref _twoFactor, value);
    }

    public string PluginDirs
    {
        get => _pluginDirs;
        set => SetProperty(ref _pluginDirs, value);
    }

    public static AppSettings Default => new()
    {
        DefaultDownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "YtDlpGui"),
        MaxConcurrentDownloads = 3,
        MaxRetries = 3,
        PreferredQuality = "best",
        PreferredFormat = "mp4",
        AudioQuality = "0",
        SocketTimeout = 30,
        Theme = "Dark",
        Language = "en"
    };
}