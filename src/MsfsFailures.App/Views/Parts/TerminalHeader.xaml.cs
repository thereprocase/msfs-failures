using System.Windows.Controls;
using System.Windows.Threading;

namespace MsfsFailures.App.Views.Parts;

public partial class TerminalHeader : UserControl
{
    private readonly DispatcherTimer _timer;

    public TerminalHeader()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateTimestamp();
        Loaded   += (_, _) => { UpdateTimestamp(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void UpdateTimestamp() =>
        TimestampText.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
}
