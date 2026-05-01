namespace MsfsFailures.App.Logging;

public sealed class InMemoryLogSink : Serilog.Core.ILogEventSink
{
    public static readonly InMemoryLogSink Instance = new();
    private readonly System.Collections.ObjectModel.ObservableCollection<LogLineVm> _lines = new();
    public System.Collections.ObjectModel.ReadOnlyObservableCollection<LogLineVm> Lines { get; }
    private const int MaxLines = 1000;
    private InMemoryLogSink() { Lines = new(_lines); }

    public void Emit(Serilog.Events.LogEvent e)
    {
        var line = new LogLineVm(e.Timestamp, e.Level.ToString().ToUpperInvariant(),
                                 e.RenderMessage());
        // Marshal to UI thread
        if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            Append(line);
        else
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => Append(line));
    }

    private void Append(LogLineVm line)
    {
        if (_lines.Count >= MaxLines) _lines.RemoveAt(0);
        _lines.Add(line);
    }
}

public sealed record LogLineVm(DateTimeOffset Timestamp, string Level, string Message);
