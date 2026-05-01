namespace MsfsFailures.Core;

/// <summary>
/// A mapping from a logical variable name to its physical address in the sim.
/// Enables WearEngine and FailureEngine to drive arbitrary aircraft without hardcoding.
/// ModelRef-scoped: each aircraft type can have different L:Var / A:Var bindings.
/// </summary>
public record VarBinding
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to ModelRef this binding applies to.</summary>
    public required Guid ModelRefId { get; init; }

    /// <summary>Logical name used internally (e.g., "oil_temp_c", "engine_1_egt_c", "battery_soh_pct").</summary>
    public required string LogicalName { get; init; }

    /// <summary>Source type (AVar, LVar, KEvent, SimConnectVar).</summary>
    public required VarSource Source { get; init; }

    /// <summary>
    /// Physical address/expression in the sim:
    /// - A:Var: "GENERAL ENG OIL TEMPERATURE:1"
    /// - L:Var: "A32NX_OIL_TEMP_1"
    /// - K:Event: "H:A32NX_FAILURE_ENG1"
    /// - SimConnectVar: registered variable name or SIMCONNECT_DEFINITION_ID
    /// Opaque to Core; consumed by Sim layer.
    /// </summary>
    public required string Expression { get; init; }
}
