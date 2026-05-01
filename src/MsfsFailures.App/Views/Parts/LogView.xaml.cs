using System.Collections.Specialized;
using System.Windows.Controls;
using MsfsFailures.App.Logging;

namespace MsfsFailures.App.Views.Parts;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();

        // Bind the ItemsControl directly to the singleton log sink, NOT the UserControl root.
        // Setting DataContext on the root would shadow the inherited HomeViewModel DataContext
        // and break the Visibility="{Binding IsLog}" binding set by HomeView.xaml.
        LogItems.DataContext = InMemoryLogSink.Instance;

        // Auto-scroll to bottom when new log lines arrive.
        // ReadOnlyObservableCollection implements INotifyCollectionChanged explicitly.
        ((INotifyCollectionChanged)InMemoryLogSink.Instance.Lines).CollectionChanged += OnLinesChanged;
    }

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            Scroller.ScrollToBottom();
    }
}
