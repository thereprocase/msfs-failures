namespace MsfsFailures.Core;

/// <summary>
/// A reference to an aircraft model type (e.g., Cessna 172, PMDG 737).
/// Component templates are keyed to ModelRef, so multiple airframes of the same type
/// can share templates. SimMatch rules allow auto-detection from MSFS state.
/// </summary>
public record ModelRef
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Human-readable name (e.g., "Cessna 172 S").</summary>
    public required string Name { get; init; }

    /// <summary>Manufacturer name for inventory/documentation.</summary>
    public required string Manufacturer { get; init; }

    /// <summary>
    /// Raw JSON string encoding match rules for auto-detection.
    /// Structure: { "ataModel": "C172", "msfsAircraftTitleContains": ["172"], "variants": [...] }
    /// Parsed by the Sim layer; Core treats as opaque.
    /// </summary>
    public required string SimMatchRules { get; init; }

    /// <summary>Timestamp when this reference was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
