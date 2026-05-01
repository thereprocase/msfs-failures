using Xunit;

namespace MsfsFailures.Sim.Tests;

/// <summary>
/// FallbackSimConnectClient ctor takes a concrete RealSimConnectClient. The
/// previous tests that instantiated a real Real client are environment-dependent
/// (succeed when MSFS happens to be running). Refactor Fallback to accept
/// <c>ISimConnectClient</c> and cover with a throwing stub before re-enabling.
/// </summary>
public sealed class FallbackSimConnectClientTests
{
    [Fact(Skip = "Refactor FallbackSimConnectClient to accept ISimConnectClient first; current ctor binds to concrete RealSimConnectClient and the test is environment-dependent.")]
    public void Placeholder() { }
}
