using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.Sim.Internal;

namespace MsfsFailures.Sim;

/// <summary>
/// Extension methods for registering <c>MsfsFailures.Sim</c> services with the DI container.
/// </summary>
public static class SimServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SimConnect integration layer as singletons.
    ///
    /// <para>
    /// Registered services:
    /// <list type="bullet">
    ///   <item><see cref="ISimBus"/> → <see cref="SimBus"/> (singleton)</item>
    ///   <item><c>ISimConnectClient</c> (internal) → <c>MockSimConnectClient</c> (singleton)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <b>To swap in the real SimConnect at integration time</b>, call this method first, then
    /// override the <c>ISimConnectClient</c> registration:
    /// <code>
    /// services.AddMsfsFailuresSim();
    /// // Override with real implementation (requires MSFS SDK DLLs in output dir):
    /// services.AddSingleton&lt;ISimConnectClient, RealSimConnectClient&gt;();
    /// </code>
    /// The last <c>AddSingleton</c> for the same interface wins in Microsoft DI.
    /// </para>
    /// </summary>
    public static IServiceCollection AddMsfsFailuresSim(this IServiceCollection services)
    {
        services.AddSingleton<ISimConnectClient, MockSimConnectClient>();
        services.AddSingleton<ISimBus, SimBus>();
        return services;
    }
}
