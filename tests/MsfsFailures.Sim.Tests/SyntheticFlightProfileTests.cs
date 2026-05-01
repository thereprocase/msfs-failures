using MsfsFailures.Sim.Internal;
using Xunit;

namespace MsfsFailures.Sim.Tests;

public sealed class SyntheticFlightProfileTests
{
    /// <summary>
    /// After exactly one nominal cycle of synthetic ticks (at 4 Hz) the phase
    /// has returned to TaxiOut, verifying the state machine loops correctly.
    /// </summary>
    [Fact]
    public void MockProfile_LoopCompletesInExpectedDuration()
    {
        // Arrange
        const double tickSec = 0.25;          // 4 Hz
        double cycleDuration = SyntheticFlightProfile.NominalCycleDurationSec;
        int    totalTicks    = (int)Math.Ceiling(cycleDuration / tickSec);

        var profile = new SyntheticFlightProfile(new Random(42));

        // Act — drive exactly one cycle worth of ticks
        for (int i = 0; i < totalTicks; i++)
            profile.Tick(tickSec);

        // Assert — should have wrapped back to TaxiOut
        Assert.Equal(FlightPhase.TaxiOut, profile.CurrentPhase);
    }

    /// <summary>
    /// Exactly one tick per loop has <see cref="MsfsFailures.Core.Wear.FlightTickSample.GroundspeedAtTouchdownKt"/> > 0.
    /// </summary>
    [Fact]
    public void MockProfile_TouchdownTickPopulatesGsAtTouchdown_OncePerCycle()
    {
        // Arrange
        const double tickSec = 0.25;
        double cycleDuration = SyntheticFlightProfile.NominalCycleDurationSec;
        int    totalTicks    = (int)Math.Ceiling(cycleDuration / tickSec);

        var profile = new SyntheticFlightProfile(new Random(42));

        // Act — collect all samples for one cycle
        int touchdownCount = 0;
        for (int i = 0; i < totalTicks; i++)
        {
            var sample = profile.Tick(tickSec);
            if (sample.GroundspeedAtTouchdownKt > 0)
                touchdownCount++;
        }

        // Assert — exactly one touchdown tick per cycle
        Assert.Equal(1, touchdownCount);
    }
}
