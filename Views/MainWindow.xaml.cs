using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using YtDlpGui.ViewModels;

namespace YtDlpGui.Views;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int useImmersiveDarkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
        }
        catch
        {
            // Fallback gracefully on older OS builds
        }
    }
}
