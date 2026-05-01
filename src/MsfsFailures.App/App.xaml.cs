using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsfsFailures.App.Logging;
using MsfsFailures.App.Services;
using MsfsFailures.App.ViewModels;
using MsfsFailures.Core.Failure;
using MsfsFailures.Core.Wear;
using MsfsFailures.Data;
using MsfsFailures.Data.Seeding;
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
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(Path.Combine(logRoot, "msfs-failures-.log"),
                              rollingInterval: RollingInterval.Day,
                              retainedFileCountLimit: 14)
                .WriteTo.Console()
                .WriteTo.Sink(InMemoryLogSink.Instance))
            .ConfigureServices(services =>
            {
                // Existing — RepositoryFleetSource reads from SQLite after migrations+seed;
                // MockFleetSource is kept as a fallback for design-time / --mock usage.
                services.AddSingleton<IFleetSource, RepositoryFleetSource>();
                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<InFlightViewModel>();
                services.AddSingleton<MainWindow>();

                // Core engines — pure functions, no I/O, safe as singletons.
                services.AddSingleton<IWearEngine, WearEngine>();
                services.AddSingleton<IFailureEngine>(_ => new FailureEngine(seed: 42));

                // Active airframe provider — singleton so all consumers share cache.
                services.AddSingleton<IActiveAirframeProvider, ActiveAirframeProvider>();

                // Background tick loop — drives WearEngine + FailureEngine from SampleStream.
                services.AddHostedService<TickHost>();

                // New layers
                services.AddMsfsFailuresData(dbPath);   // from MsfsFailures.Data
                services.AddMsfsFailuresSim();          // from MsfsFailures.Sim
            })
            .Build();

        await _host.StartAsync();

        // Migrate schema and seed demo data on first run.
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
            db.Database.Migrate();
            await SeedIfEmpty.ApplyAsync(
                db,
                scope.ServiceProvider.GetRequiredService<ILogger<App>>());
        }

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
