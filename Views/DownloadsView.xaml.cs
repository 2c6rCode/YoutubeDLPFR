using System.IO;
using System.Windows;
using System.Windows.Controls;
using YtDlpGui.ViewModels;

namespace YtDlpGui.Views;

public partial class DownloadsView : UserControl
{
    public DownloadsView()
    {
        InitializeComponent();
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not DownloadsViewModel vm) return;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var file = files[0];
                if (File.Exists(file))
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        vm.LoadUrlsFromContent(content, Path.GetFileName(file));
                    }
                    catch
                    {
                        // Graceful fallback
                    }
                }
            }
        }
        else if (e.Data.GetDataPresent(DataFormats.Text))
        {
            var text = (string)e.Data.GetData(DataFormats.Text);
            if (!string.IsNullOrWhiteSpace(text))
            {
                vm.LoadUrlsFromContent(text);
            }
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }
}
