namespace MsfsFailures.Core;

/// <summary>
/// A wear/failure accelerator: a category-wide rule that multiplies component wear rate
/// based on operational stress (e.g., oil_temp > 115 °C → 1.5x wear).
/// Formula is stored as raw JSON; WearEngine interprets and evaluates.
/// </summary>
public record Accelerator
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Component category this accelerator applies to (e.g., Engine, OilSystem).</summary>
    public required ComponentCategory Category { get; init; }

    /// <summary>
    /// Operand variable name (e.g., "oil_temp_C", "manifold_pressure_inHg", "hard_landing_g").
    /// Resolved by Sim layer into actual values each tick.
    /// </summary>
    public required string Variable { get; init; }

    /// <summary>
    /// Raw JSON string encoding the accelerator formula.
    /// Structure: { "type": "linear|exponential|custom", "threshold": 115, "scale": 1.5, ... }
    /// WearEngine interprets to compute multiplier [1..N].
    /// </summary>
    public required string Formula { get; init; }
}
