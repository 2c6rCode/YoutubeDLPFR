using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace YtDlpGui.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;
}