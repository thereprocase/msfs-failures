using System.Windows;
using System.Windows.Controls;

namespace MsfsFailures.App.Views.Parts;

public partial class Placeholder : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(Placeholder),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Placeholder p) p.BodyText.Text = (string)e.NewValue;
    }

    public Placeholder() => InitializeComponent();
}
