using System.Collections.Specialized;
using System.Windows.Controls;
using MsfsFailures.App.Logging;

namespace MsfsFailures.App.Views.Parts;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();

        // Bind directly to the singleton in-memory sink.
        DataContext = InMemoryLogSink.Instance;

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
