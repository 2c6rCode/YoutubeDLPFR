using System.Windows;
using YtDlpGui.ViewModels;

namespace YtDlpGui.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}