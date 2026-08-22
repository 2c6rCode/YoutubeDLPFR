using System.Windows;
using System.Windows.Controls;
using YtDlpGui.Models;

namespace YtDlpGui.Controls;

public class DownloadItemControl : ContentControl
{
    static DownloadItemControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DownloadItemControl), new FrameworkPropertyMetadata(typeof(DownloadItemControl)));
    }

    public static readonly DependencyProperty DownloadItemProperty =
        DependencyProperty.Register(nameof(DownloadItem), typeof(DownloadItem), typeof(DownloadItemControl), new PropertyMetadata(null));

    public DownloadItem DownloadItem
    {
        get => (DownloadItem)GetValue(DownloadItemProperty);
        set => SetValue(DownloadItemProperty, value);
    }

    public static readonly DependencyProperty ShowActionsProperty =
        DependencyProperty.Register(nameof(ShowActions), typeof(bool), typeof(DownloadItemControl), new PropertyMetadata(true));

    public bool ShowActions
    {
        get => (bool)GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    public static readonly DependencyProperty CompactModeProperty =
        DependencyProperty.Register(nameof(CompactMode), typeof(bool), typeof(DownloadItemControl), new PropertyMetadata(false));

    public bool CompactMode
    {
        get => (bool)GetValue(CompactModeProperty);
        set => SetValue(CompactModeProperty, value);
    }
}