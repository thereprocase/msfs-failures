using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace MsfsFailures.App.Views.InFlight;

[ContentProperty(nameof(Body))]
public partial class Block : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(Block),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty RightProperty =
        DependencyProperty.Register(nameof(Right), typeof(object), typeof(Block),
            new PropertyMetadata(null, OnRightChanged));

    public object? Right
    {
        get => GetValue(RightProperty);
        set => SetValue(RightProperty, value);
    }

    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(nameof(Body), typeof(object), typeof(Block),
            new PropertyMetadata(null, OnBodyChanged));

    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public static readonly DependencyProperty BodyPaddingProperty =
        DependencyProperty.Register(nameof(BodyPadding), typeof(Thickness), typeof(Block),
            new PropertyMetadata(new Thickness(14), OnBodyPaddingChanged));

    public Thickness BodyPadding
    {
        get => (Thickness)GetValue(BodyPaddingProperty);
        set => SetValue(BodyPaddingProperty, value);
    }

    public Block() => InitializeComponent();

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Block b) b.TitleText.Text = "[" + (e.NewValue as string ?? string.Empty) + "]";
    }

    private static void OnRightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Block b) b.RightContent.Content = e.NewValue;
    }

    private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Block b) b.BodyContent.Content = e.NewValue;
    }

    private static void OnBodyPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Block b) b.BodyContent.Margin = (Thickness)e.NewValue;
    }
}
