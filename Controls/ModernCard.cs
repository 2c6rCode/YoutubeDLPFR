using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YtDlpGui.Controls;

public class ModernCard : ContentControl
{
    static ModernCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ModernCard), new FrameworkPropertyMetadata(typeof(ModernCard)));
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ModernCard), new PropertyMetadata(new CornerRadius(8)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty ElevationProperty =
        DependencyProperty.Register(nameof(Elevation), typeof(int), typeof(ModernCard), new PropertyMetadata(1));

    public int Elevation
    {
        get => (int)GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }
}