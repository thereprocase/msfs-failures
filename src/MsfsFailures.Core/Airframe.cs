namespace MsfsFailures.Core;

/// <summary>
/// An individual aircraft tracked in the fleet.
/// Persists hours, cycles, and references a ModelRef for component templates.
/// </summary>
public record Airframe
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Tail number (e.g., "N12345"). Not enforced unique but user-intended to be unique-ish.</summary>
    public required string Tail { get; init; }

    /// <summary>Aircraft type in ICAO form (e.g., "C172", "B737").</summary>
    public required string Type { get; init; }

    /// <summary>Foreign key to ModelRef; defines which component templates apply.</summary>
    public required Guid ModelRefId { get; init; }

    /// <summary>Cumulative Hobbs hours (engine running time).</summary>
    public required double TotalHobbsHours { get; init; }

    /// <summary>Cumulative cycles (takeoff/landing pairs, depending on aircraft).</summary>
    public required int TotalCycles { get; init; }

    /// <summary>When this airframe record was first created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
