using Microsoft.Extensions.Logging.Abstractions;
using MsfsFailures.Sim.Internal;
using Xunit;

namespace MsfsFailures.Sim.Tests;

/// <summary>
/// Unit tests for <see cref="FallbackSimConnectClient"/>.
/// Verifies that a failed real-client open transparently switches to the mock.
/// </summary>
public sealed class FallbackSimConnectClientTests
{
    /// <summary>
    /// When the real client throws on OpenAsync (MSFS not running, COM failure, etc.)
    /// the fallback client should open successfully via the mock path and not re-throw.
    /// </summary>
    [Fact]
    public async Task OpenAsync_RealThrows_FallsBackToMock()
    {
        var realLogger     = NullLogger<RealSimConnectClient>.Instance;
        var mockLogger     = NullLogger<MockSimConnectClient>.Instance;
        var fallbackLogger = NullLogger<FallbackSimConnectClient>.Instance;

        // Use a real RealSimConnectClient — it will fail because we're not on Windows
        // with MSFS running, so SimConnect_Open throws.  That's exactly the scenario
        // we want to exercise.
        var real     = new RealSimConnectClient(realLogger);
        var mock     = new MockSimConnectClient(mockLogger, randomSeed: 0);
        var fallback = new FallbackSimConnectClient(real, mock, fallbackLogger);

        // Should not throw — must fall back to mock
        var ex = await Record.ExceptionAsync(() => fallback.OpenAsync());

        Assert.Null(ex);

        // Verify that samples flow (mock emits after OpenAsync)
        var samplesReceived = new List<MsfsFailures.Core.Wear.FlightTickSample>();
        fallback.SampleProduced += (_, e) => samplesReceived.Add(e.Sample);

        await Task.Delay(700);   // mock starts at 4 Hz — expect at least 2 samples in 700 ms

        await fallback.CloseAsync();
        await fallback.DisposeAsync();

        Assert.True(samplesReceived.Count >= 1,
            $"Expected at least 1 sample from mock path; got {samplesReceived.Count}.");
    }

    /// <summary>
    /// When the real client throws, the fallback emits a <c>Connected</c> event (from the mock).
    /// The event fires synchronously inside OpenAsync so we must subscribe before calling it.
    /// </summary>
    [Fact]
    public async Task OpenAsync_RealThrows_ConnectedEventRaised()
    {
        var real     = new RealSimConnectClient(NullLogger<RealSimConnectClient>.Instance);
        var mock     = new MockSimConnectClient(NullLogger<MockSimConnectClient>.Instance, randomSeed: 0);
        var fallback = new FallbackSimConnectClient(real, mock, NullLogger<FallbackSimConnectClient>.Instance);

        // Subscribe BEFORE OpenAsync — mock fires Connected synchronously during its OpenAsync.
        SimConnectedEventArgs? connectedArgs = null;
        fallback.Connected += (_, e) => connectedArgs = e;

        // OpenAsync will fail real, succeed mock.  After the await the Connected event
        // has already fired (MockSimConnectClient fires it inside the awaited Task.Delay).
        await fallback.OpenAsync();

        // Give the background mock timer a moment if needed (it actually fires during OpenAsync)
        await Task.Delay(50);

        Assert.NotNull(connectedArgs);
        Assert.Contains("Mock", connectedArgs!.SimVersion, StringComparison.OrdinalIgnoreCase);

        await fallback.CloseAsync();
        await fallback.DisposeAsync();
    }
}
