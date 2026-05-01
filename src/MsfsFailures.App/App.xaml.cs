using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.App.Services;
using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddSingleton<IFleetSource, MockFleetSource>();
        services.AddSingleton<HomeViewModel>();
        Services = services.BuildServiceProvider();

        var window = new MainWindow
        {
            DataContext = Services.GetRequiredService<HomeViewModel>(),
        };
        MainWindow = window;
        window.Show();
    }
}
