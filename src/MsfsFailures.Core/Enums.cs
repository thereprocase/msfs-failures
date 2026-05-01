namespace MsfsFailures.Core;

/// <summary>
/// Component categories for wear tracking and failure modeling.
/// Aligns with physical systems in typical aircraft.
/// </summary>
public enum ComponentCategory
{
    Engine,
    HotSection,
    Compressor,
    OilSystem,
    FuelSystem,
    Propeller,
    GearBrakes,
    Tires,
    Battery,
    Hydraulic,
    Avionics,
    Other
}

/// <summary>
/// Consumable types tracked in the fleet.
/// </summary>
public enum ConsumableKind
{
    None,
    Oil,
    HydraulicFluid,
    BrakePad,
    Tire,
    BatterySoh
}

/// <summary>
/// How a failure mode is injected into the sim.
/// Maps to the communication layer (SimConnect, L:Var, K:Event).
/// </summary>
public enum SimBindingKind
{
    /// <summary>SimConnect event (e.g., TOGGLE_MAGNETO_1_FAILURE).</summary>
    SimConnectEvent,

    /// <summary>L:Var write via MobiFlight or other WASM module.</summary>
    LVarWrite,

    /// <summary>K:Event write (e.g., H: namespace in FBW).</summary>
    KEventWrite,

    /// <summary>Tracked in Core only; no sim injection (internal-only failures).</summary>
    InternalOnly
}

/// <summary>
/// Failure severity from the crew's perspective.
/// Guides MEL deferral eligibility and priority.
/// </summary>
public enum FailureSeverity
{
    /// <summary>Annunciator light; non-urgent.</summary>
    Annunciator,

    /// <summary>Warning amber; should be investigated soon.</summary>
    Warning,

    /// <summary>Caution amber; crew discretion on when to address.</summary>
    Caution,

    /// <summary>Hard failure; grounds aircraft without repair.</summary>
    Grounding
}

/// <summary>
/// Status of a reported failure (squawk).
/// MEL logic: Open → Deferrable depending on severity.
/// Deferred → Closed once repaired.
/// </summary>
public enum SquawkStatus
{
    /// <summary>Failure reported and active.</summary>
    Open,

    /// <summary>Failure deferred under MEL; expiration tracked.</summary>
    Deferred,

    /// <summary>Failure rectified and closed.</summary>
    Closed
}

/// <summary>
/// Where a variable binding pulls data from.
/// Used by the sim layer to know how to read/write aircraft state.
/// </summary>
public enum VarSource
{
    /// <summary>Simulation variable (A:VarName).</summary>
    AVar,

    /// <summary>Local variable in WASM module (L:VarName).</summary>
    LVar,

    /// <summary>Key event (K: namespace, e.g. FBW _FAILURES_ events).</summary>
    KEvent,

    /// <summary>SimConnect native variable (registered via SimConnect).</summary>
    SimConnectVar
}
