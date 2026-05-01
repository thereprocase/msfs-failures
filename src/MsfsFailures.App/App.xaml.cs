using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MsfsFailures.App.Logging;
using MsfsFailures.App.Services;
using MsfsFailures.App.ViewModels;
using MsfsFailures.Data;
using MsfsFailures.Sim;
using Serilog;

namespace MsfsFailures.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MsfsFailures", "fleet.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var logRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MsfsFailures", "logs");
        Directory.CreateDirectory(logRoot);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog((ctx, lc) => lc
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File(Path.Combine(logRoot, "msfs-failures-.log"),
                              rollingInterval: RollingInterval.Day,
                              retainedFileCountLimit: 14)
                .WriteTo.Console()
                .WriteTo.Sink(InMemoryLogSink.Instance))
            .ConfigureServices(services =>
            {
                // Existing
                services.AddSingleton<IFleetSource, MockFleetSource>();
                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<MainWindow>();

                // New layers
                services.AddMsfsFailuresData(dbPath);   // from MsfsFailures.Data
                services.AddMsfsFailuresSim();          // from MsfsFailures.Sim
            })
            .Build();

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = _host.Services.GetRequiredService<HomeViewModel>();
        MainWindow = window;
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
