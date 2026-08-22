using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YtDlpGui.Controls;

public class ModernTextBox : TextBox
{
    static ModernTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ModernTextBox), new FrameworkPropertyMetadata(typeof(ModernTextBox)));
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ModernTextBox), new PropertyMetadata(new CornerRadius(6)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(ModernTextBox), new PropertyMetadata(string.Empty));

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(ModernTextBox), new PropertyMetadata(null));

    public Geometry Icon
    {
        get => (Geometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(ModernTextBox), new PropertyMetadata(16.0));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty ValidationErrorProperty =
        DependencyProperty.Register(nameof(ValidationError), typeof(string), typeof(ModernTextBox), new PropertyMetadata(string.Empty));

    public string ValidationError
    {
        get => (string)GetValue(ValidationErrorProperty);
        set => SetValue(ValidationErrorProperty, value);
    }

    public static readonly DependencyProperty HasValidationErrorProperty =
        DependencyProperty.Register(nameof(HasValidationError), typeof(bool), typeof(ModernTextBox), new PropertyMetadata(false));

    public bool HasValidationError
    {
        get => (bool)GetValue(HasValidationErrorProperty);
        set => SetValue(HasValidationErrorProperty, value);
    }
}