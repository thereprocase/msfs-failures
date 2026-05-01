namespace MsfsFailures.Core;

/// <summary>
/// A flight session: a discrete period during which an airframe was operated.
/// Tracks Hobbs, cycles, stress events (hard landings, overtemps, overspeeds), and G-loads.
/// </summary>
public record Session
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to Airframe being flown.</summary>
    public required Guid AirframeId { get; init; }

    /// <summary>When the flight session started (engines on, first instrumentation alive).</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>When the flight session ended. Null if still in progress.</summary>
    public required DateTimeOffset? EndedAt { get; init; }

    /// <summary>Hobbs reading at session start.</summary>
    public required double HobbsStart { get; init; }

    /// <summary>Hobbs reading at session end. Null if session still in progress.</summary>
    public required double? HobbsEnd { get; init; }

    /// <summary>Peak G-load experienced during the session (absolute value, including negative G).</summary>
    public required double MaxG { get; init; }

    /// <summary>Count of landings with vertical speed exceeding a threshold (typically > 600 fpm).</summary>
    public required int HardLandings { get; init; }

    /// <summary>
    /// Raw JSON string listing overtemp/overspeed/overG events that occurred.
    /// Structure: [{ "event": "oil_temp_overheat", "value": 125.5, "limit": 120, "ts": "2026-05-01T14:32:00Z" }, ...]
    /// Consumed by accelerator evaluation; Core stores as opaque.
    /// </summary>
    public required string OvertempEvents { get; init; }
}
