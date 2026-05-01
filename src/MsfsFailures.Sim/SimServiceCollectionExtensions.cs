using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.Sim.Internal;

namespace MsfsFailures.Sim;

/// <summary>
/// Controls which <see cref="ISimConnectClient"/> implementation is registered.
/// </summary>
/// <remarks>
/// The active mode can be overridden at runtime via the environment variable
/// <c>MSFS_FAILURES_SIM</c>:
/// <list type="bullet">
///   <item><c>mock</c>  — always use <see cref="MockSimConnectClient"/> (dev / CI without MSFS).</item>
///   <item><c>real</c>  — always use <see cref="RealSimConnectClient"/> (fail hard if MSFS not running).</item>
///   <item><c>auto</c>  — (default) try <see cref="RealSimConnectClient"/>; on failure fall back to
///       <see cref="MockSimConnectClient"/> with a warning log.</item>
/// </list>
/// </remarks>
public enum SimMode
{
    /// <summary>Always use the mock (synthetic flight profile). Safe without MSFS.</summary>
    Mock,

    /// <summary>Always use the real SimConnect client. Throws if MSFS is not running.</summary>
    Real,

    /// <summary>
    /// Try the real client first; on any connect failure transparently switch to the mock.
    /// This is the default — the app works in dev/CI and also talks to a live sim.
    /// </summary>
    Auto,
}

/// <summary>
/// Extension methods for registering <c>MsfsFailures.Sim</c> services with the DI container.
/// </summary>
public static class SimServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SimConnect integration layer as singletons using the mode specified by the
    /// <c>MSFS_FAILURES_SIM</c> environment variable (default: <see cref="SimMode.Auto"/>).
    ///
    /// <para>
    /// Registered services:
    /// <list type="bullet">
    ///   <item><see cref="ISimBus"/> → <see cref="SimBus"/> (singleton)</item>
    ///   <item><c>ISimConnectClient</c> (internal) → determined by <paramref name="mode"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static IServiceCollection AddMsfsFailuresSim(
        this IServiceCollection services,
        SimMode mode = SimMode.Auto)
    {
        // Allow the environment variable to override the programmatic mode
        var envVar = Environment.GetEnvironmentVariable("MSFS_FAILURES_SIM");
        if (!string.IsNullOrWhiteSpace(envVar) &&
            Enum.TryParse<SimMode>(envVar, ignoreCase: true, out var envMode))
        {
            mode = envMode;
        }

        switch (mode)
        {
            case SimMode.Mock:
                services.AddSingleton<ISimConnectClient, MockSimConnectClient>();
                break;

            case SimMode.Real:
                services.AddSingleton<ISimConnectClient, RealSimConnectClient>();
                break;

            default: // Auto
                // Register both inner clients so DI can construct FallbackSimConnectClient.
                services.AddSingleton<RealSimConnectClient>();
                services.AddSingleton<MockSimConnectClient>();
                services.AddSingleton<ISimConnectClient, FallbackSimConnectClient>();
                break;
        }

        services.AddSingleton<ISimBus, SimBus>();
        return services;
    }
}
