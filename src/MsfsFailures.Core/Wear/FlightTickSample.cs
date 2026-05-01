namespace MsfsFailures.Core.Wear;

/// <summary>
/// A single 4-Hz state snapshot pushed by the Sim layer into the WearEngine.
/// All fields use SI-adjacent aviation units (knots, fpm, °C, etc.) as labeled.
/// Fields that do not apply to the current aircraft type are set to -1.
/// </summary>
public sealed record FlightTickSample(
    /// <summary>Wall-clock timestamp of this sample.</summary>
    DateTimeOffset Timestamp,

    /// <summary>True if the aircraft weight-on-wheels switch is active.</summary>
    bool OnGround,

    /// <summary>Indicated airspeed in knots.</summary>
    double IasKt,

    /// <summary>GPS groundspeed in knots.</summary>
    double GsKt,

    /// <summary>Vertical speed in feet per minute (positive = climbing).</summary>
    double VerticalSpeedFpm,

    /// <summary>Normal G-load (1.0 = level flight).</summary>
    double GLoad,

    /// <summary>Outside air temperature in degrees Celsius.</summary>
    double OatC,

    /// <summary>Engine RPM (piston) or shaft RPM. Used for Hobbs gating.</summary>
    double EngineRpm,

    /// <summary>Engine N1 as a percentage (turbine). Used for Hobbs gating and oil burn scaling.</summary>
    double EngineN1Pct,

    /// <summary>
    /// Primary thermal channel in degrees Celsius.
    /// For turbines: ITT. For pistons: EGT. Set to -1 if not available.
    /// </summary>
    double IttC,

    /// <summary>Engine torque in ft-lb. Set to -1 if not available (piston engines typically).</summary>
    double TorqueFtLb,

    /// <summary>Fuel flow in pounds per hour.</summary>
    double FuelFlowPph,

    /// <summary>Oil temperature in degrees Celsius.</summary>
    double OilTempC,

    /// <summary>Oil pressure in PSI.</summary>
    double OilPressurePsi,

    /// <summary>
    /// Groundspeed at touchdown in knots.
    /// Populated only on touchdown event ticks (value > 0); zero on all other ticks.
    /// </summary>
    double GroundspeedAtTouchdownKt,

    /// <summary>
    /// Brake energy accumulated since the last tick, in joules.
    /// Computed by the Sim layer from brake-force × velocity integration.
    /// </summary>
    double BrakeEnergyJoules);
