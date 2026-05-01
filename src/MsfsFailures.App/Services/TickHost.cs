using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsfsFailures.Core.Failure;
using MsfsFailures.Core.Wear;
using MsfsFailures.Data.Repositories;
using MsfsFailures.Sim;

namespace MsfsFailures.App.Services;

/// <summary>
/// Background service that consumes <see cref="ISimBus.SampleStream"/> at 4 Hz,
/// runs <see cref="IWearEngine.Tick"/> + <see cref="IFailureEngine.Roll"/> each tick,
/// and persists accumulated deltas to SQLite via <see cref="IFleetRepository"/> every
/// ~5 seconds (20 ticks at 4 Hz) to avoid hammering the DB at full sample rate.
///
/// Session lifecycle: StartSessionAsync on first Connected status; EndSessionAsync on Disconnected.
///
/// Aircraft lookup: v1 hardcodes tail "N208RC" (the seeded demo aircraft).
/// TODO: derive from sim aircraft detection (TITLE/ATC_MODEL) once identity-matching is wired.
/// </summary>
public sealed class TickHost : BackgroundService
{
    // Batch every N ticks before writing to SQLite (~5 s at 4 Hz).
    private const int BatchFlushThresholdTicks = 20;

    private readonly ISimBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TickHost> _log;
    private readonly IWearEngine _wear;
    private readonly IFailureEngine _failure;
    private readonly IActiveAirframeProvider _airframeProvider;

    // Session state — managed on the subscription thread (sequential per-sample).
    private Guid? _currentSessionId;
    private double _sessionHobbsStart;
    private double _currentHobbs;
    private double _maxG = 1.0;
    private int _hardLandings;
    private bool _airframeWarnedMissing;

    // Batch accumulator fields — guarded by sequential reactive subscription.
    private int _batchTickCount;
    private double _batchHobbsDelta;
    private int _batchCyclesDelta;
    private readonly Dictionary<Guid, double> _batchComponentWear  = new();
    private readonly Dictionary<Guid, double> _batchConsumableDelta = new();

    public TickHost(
        ISimBus bus,
        IServiceScopeFactory scopeFactory,
        ILogger<TickHost> log,
        IWearEngine wear,
        IFailureEngine failure,
        IActiveAirframeProvider airframeProvider)
    {
        _bus               = bus;
        _scopeFactory      = scopeFactory;
        _log               = log;
        _wear              = wear;
        _failure           = failure;
        _airframeProvider  = airframeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("TickHost starting.");

        // Initiate sim connection on startup. The bus manages reconnect state internally.
        // On real hardware this would await actual SimConnect; on mock it resolves in ~300 ms.
        await _bus.ConnectAsync(stoppingToken).ConfigureAwait(false);

        // Bridge the hot IObservable<FlightTickSample> into an async-consumable channel.
        // This avoids blocking the Rx subscription thread and gives us clean async/await.
        var channel = Channel.CreateBounded<FlightTickSample>(
            new BoundedChannelOptions(32)
            {
                FullMode      = BoundedChannelFullMode.DropOldest,
                SingleReader  = true,
                SingleWriter  = false,
            });

        // Subscribe to status stream to manage session lifecycle.
        using var statusSub = _bus.StatusStream.Subscribe(
            status => OnStatus(status, stoppingToken),
            ex => _log.LogError(ex, "TickHost: StatusStream error."));

        // Forward samples into the channel; complete the channel on stop.
        using var sampleSub = _bus.SampleStream.Subscribe(
            sample => channel.Writer.TryWrite(sample),
            ex =>
            {
                _log.LogError(ex, "TickHost: SampleStream error.");
                channel.Writer.TryComplete(ex);
            },
            () => channel.Writer.TryComplete());

        // Register cancellation to complete the channel so the reader exits cleanly.
        stoppingToken.Register(() => channel.Writer.TryComplete());

        // Process samples sequentially as they arrive.
        await foreach (var sample in channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await OnSampleAsync(sample, stoppingToken).ConfigureAwait(false);
        }

        _log.LogInformation("TickHost stopped.");
    }

    // ── Status handling ──────────────────────────────────────────────────────

    private void OnStatus(SimStatus status, CancellationToken ct)
    {
        if (status.State == SimConnectionState.Connected && _currentSessionId is null)
        {
            // Fire-and-forget: open a session asynchronously without blocking the reactive callback.
            _ = StartSessionFireAndForgetAsync(ct);
        }
        else if (status.State == SimConnectionState.Error && _currentSessionId.HasValue)
        {
            // Transient SimConnect errors (e.g. unsupported A:Var subscription) must NOT end
            // the session.  Samples may still flow after the error; keep the session alive and
            // wait for either more samples or a true Offline transition.
            _log.LogWarning(
                "TickHost: SimConnect error while session {SessionId} is active — keeping session alive. " +
                "Error: {ErrorMessage}",
                _currentSessionId.Value,
                status.ErrorMessage ?? "(no detail)");
        }
        else if (status.State == SimConnectionState.Offline && _currentSessionId.HasValue)
        {
            // True disconnect (MSFS exited or SimConnect closed) — flush batch and end session.
            _ = EndSessionFireAndForgetAsync(ct);
        }
    }

    private async Task StartSessionFireAndForgetAsync(CancellationToken ct)
    {
        try
        {
            // TODO: derive airframe from sim aircraft detection (TITLE/ATC_MODEL)
            var airframeId = await ResolveAirframeIdAsync(ct);
            if (airframeId is null) return;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
            var session = await repo.StartSessionAsync(airframeId.Value, _currentHobbs, ct);
            _currentSessionId = session.Id;
            _sessionHobbsStart = _currentHobbs;
            _maxG = 1.0;
            _hardLandings = 0;
            _log.LogInformation("TickHost: connected, started session {SessionId} for airframe at hobbs {Hobbs:F1}.",
                session.Id, _currentHobbs);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "TickHost: failed to start session.");
        }
    }

    private async Task EndSessionFireAndForgetAsync(CancellationToken ct)
    {
        var sessionId = _currentSessionId;
        _currentSessionId = null;

        // Flush any pending batch first.
        await FlushBatchAsync(ct).ConfigureAwait(false);

        if (sessionId.HasValue)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
                await repo.EndSessionAsync(sessionId.Value, _currentHobbs, _maxG, _hardLandings, ct);
                _log.LogInformation(
                    "TickHost: session {SessionId} ended. Hobbs={HobbsEnd:F2} MaxG={MaxG:F2} HardLandings={HL}.",
                    sessionId.Value, _currentHobbs, _maxG, _hardLandings);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TickHost: failed to end session {SessionId}.", sessionId.Value);
            }
        }
    }

    // ── Sample processing ────────────────────────────────────────────────────

    private static readonly TimeSpan TickDt = TimeSpan.FromMilliseconds(250); // nominal 4 Hz

    private async Task OnSampleAsync(FlightTickSample sample, CancellationToken ct)
    {
        try
        {
            // TODO: derive airframe from sim aircraft detection (TITLE/ATC_MODEL)
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();

            // TODO: derive airframe from sim aircraft detection (TITLE/ATC_MODEL)
            // v1 fallback: try well-known demo tail, then take first airframe in DB.
            var airframe = await repo.GetAirframeByTailAsync("N208RC", ct);
            if (airframe is null)
            {
                var all = await repo.GetAllAirframesAsync(ct);
                airframe = all.Count > 0 ? all[0] : null;
            }

            if (airframe is null)
            {
                if (!_airframeWarnedMissing)
                {
                    _log.LogWarning("TickHost: no airframe found in DB; skipping ticks. " +
                                    "Seed data may be missing.");
                    _airframeWarnedMissing = true;
                }
                return;
            }
            _airframeWarnedMissing = false;

            // Build Core-domain objects from Data entities for the engine calls.
            var components  = await repo.GetComponentsForAirframeAsync(airframe.Id, ct);
            var consumables = await repo.GetConsumablesForAirframeAsync(airframe.Id, ct);
            var templates   = await repo.GetTemplatesForModelAsync(airframe.ModelRefId, ct);
            var templateIds = templates.Select(t => t.Id).ToList();
            var modes       = await repo.GetFailureModesForTemplatesAsync(templateIds, ct);
            var accelerators = await repo.GetAcceleratorsAsync(ct);

            var coreAirframe    = MapAirframe(airframe);
            var coreComponents  = components.Select(MapComponent).ToList();
            var coreConsumables = consumables.Select(MapConsumable).ToList();
            var coreTemplates   = templates.Select(MapTemplate).ToList();
            var coreModes       = modes.Select(MapFailureMode).ToList();
            var coreAccelerators = accelerators.Select(MapAccelerator).ToList();

            // Run wear engine.
            var wearResult = _wear.Tick(
                coreAirframe,
                coreComponents,
                coreConsumables,
                coreAccelerators,
                sample,
                TickDt);

            // Track session-level aggregates.
            _currentHobbs += wearResult.HobbsHoursDelta;
            _hardLandings += wearResult.CyclesDelta > 0 &&
                             Math.Abs(sample.VerticalSpeedFpm) > 500 ? 1 : 0;
            if (Math.Abs(sample.GLoad) > _maxG) _maxG = Math.Abs(sample.GLoad);

            // Build sample-vars dictionary for FailureEngine.
            var sampleVars = BuildSampleVars(sample);

            // Run failure engine (roll — results logged but not yet persisted as squawks in v1).
            var failureResult = _failure.Roll(
                coreAirframe,
                coreComponents,
                coreTemplates,
                coreModes,
                coreAccelerators,
                sampleVars,
                TickDt);

            if (failureResult.Triggered.Count > 0)
            {
                foreach (var tf in failureResult.Triggered)
                {
                    _log.LogWarning(
                        "TickHost: failure triggered — component={ComponentId} mode={FailureModeId} reason={Reason}",
                        tf.ComponentId, tf.FailureModeId, tf.ReasonShort);
                }
            }

            // Accumulate batch deltas.
            AccumulateBatch(wearResult);

            // Flush batch every N ticks.
            if (_batchTickCount >= BatchFlushThresholdTicks)
            {
                await FlushBatchAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw; // let the caller handle cancellation
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "TickHost: error processing sample.");
        }
    }

    // ── Batch accumulation ───────────────────────────────────────────────────

    private void AccumulateBatch(WearTickResult result)
    {
        _batchTickCount++;
        _batchHobbsDelta   += result.HobbsHoursDelta;
        _batchCyclesDelta  += result.CyclesDelta;

        foreach (var (id, delta) in result.ComponentWearDeltas)
        {
            _batchComponentWear.TryGetValue(id, out double existing);
            _batchComponentWear[id] = existing + delta;
        }

        foreach (var (id, delta) in result.ConsumableLevelDeltas)
        {
            _batchConsumableDelta.TryGetValue(id, out double existing);
            _batchConsumableDelta[id] = existing + delta;
        }
    }

    private async Task FlushBatchAsync(CancellationToken ct)
    {
        if (_batchTickCount == 0) return;

        // Snapshot and reset the batch atomically (single-threaded subscription, no lock needed).
        double hobbs   = _batchHobbsDelta;
        int cycles     = _batchCyclesDelta;
        var compWear   = new Dictionary<Guid, double>(_batchComponentWear);
        var consLevels = new Dictionary<Guid, double>(_batchConsumableDelta);

        _batchHobbsDelta  = 0;
        _batchCyclesDelta = 0;
        _batchComponentWear.Clear();
        _batchConsumableDelta.Clear();
        _batchTickCount = 0;

        try
        {
            var airframeId = await ResolveAirframeIdAsync(ct);
            if (airframeId is null) return;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
            await repo.ApplyTickResultAsync(airframeId.Value, hobbs, cycles, compWear, consLevels, ct);

            _log.LogDebug(
                "TickHost: batch flushed — hobbs+{Hobbs:F4} cycles+{Cycles} compWear={CW} consumables={CS}.",
                hobbs, cycles, compWear.Count, consLevels.Count);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "TickHost: batch flush failed.");
        }
    }

    // ── Airframe resolution ──────────────────────────────────────────────────

    private async Task<Guid?> ResolveAirframeIdAsync(CancellationToken ct)
    {
        // Delegate to the provider.
        return await _airframeProvider.GetActiveAirframeIdAsync(ct);
    }

    // ── Data → Core mapping ──────────────────────────────────────────────────

    private static Core.Airframe MapAirframe(Data.Entities.Airframe a) =>
        new()
        {
            Id              = a.Id,
            Tail            = a.Tail,
            Type            = a.Type,
            ModelRefId      = a.ModelRefId,
            TotalHobbsHours = a.TotalHobbsHours,
            TotalCycles     = a.TotalCycles,
            CreatedAt       = a.CreatedAt,
        };

    private static Core.Component MapComponent(Data.Entities.Component c) =>
        new()
        {
            Id            = c.Id,
            AirframeId    = c.AirframeId,
            TemplateId    = c.TemplateId,
            Hours         = c.Hours,
            Cycles        = c.Cycles,
            Wear          = c.Wear,
            Condition     = c.Condition,
            LastServicedAt = c.LastServicedAt,
            InstalledAt   = c.InstalledAt,
        };

    private static Core.Consumable MapConsumable(Data.Entities.Consumable c) =>
        new()
        {
            Id         = c.Id,
            AirframeId = c.AirframeId,
            Kind       = (Core.ConsumableKind)c.Kind,
            Level      = c.Level,
            Capacity   = c.Capacity,
            LastTopUpAt = c.LastTopUpAt,
        };

    private static Core.ComponentTemplate MapTemplate(Data.Entities.ComponentTemplate t) =>
        new()
        {
            Id                   = t.Id,
            ModelRefId           = t.ModelRefId,
            Category             = (Core.ComponentCategory)t.Category,
            Name                 = t.Name,
            MtbfHours            = t.MtbfHours,
            WearCurve            = t.WearCurveJson,
            ConsumableKind       = (Core.ConsumableKind)t.ConsumableKind,
            ReplaceIntervalHours = t.ReplaceIntervalHours ?? 0.0,
            ReplaceIntervalCycles = t.ReplaceIntervalCycles ?? 0,
        };

    private static Core.FailureMode MapFailureMode(Data.Entities.FailureMode f) =>
        new()
        {
            Id               = f.Id,
            TemplateId       = f.TemplateId,
            Name             = f.Name,
            SimBindingKind   = (Core.SimBindingKind)f.SimBindingKind,
            SimBindingPayload = f.SimBindingPayload,
            Severity         = (Core.FailureSeverity)f.Severity,
            RepairHours      = f.RepairHours,
            MelDeferrable    = f.MelDeferrable,
        };

    private static Core.Accelerator MapAccelerator(Data.Entities.Accelerator a) =>
        new()
        {
            Id       = a.Id,
            Category = (Core.ComponentCategory)a.Category,
            Variable = a.Variable,
            Formula  = a.FormulaJson,
        };

    private static IReadOnlyDictionary<string, double> BuildSampleVars(FlightTickSample s) =>
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["IasKt"]                    = s.IasKt,
            ["GsKt"]                     = s.GsKt,
            ["VerticalSpeedFpm"]         = s.VerticalSpeedFpm,
            ["GLoad"]                    = s.GLoad,
            ["OatC"]                     = s.OatC,
            ["EngineRpm"]                = s.EngineRpm,
            ["EngineN1Pct"]              = s.EngineN1Pct,
            ["IttC"]                     = s.IttC,
            ["TorqueFtLb"]               = s.TorqueFtLb,
            ["FuelFlowPph"]              = s.FuelFlowPph,
            ["OilTempC"]                 = s.OilTempC,
            ["OilPressurePsi"]           = s.OilPressurePsi,
            ["GroundspeedAtTouchdownKt"] = s.GroundspeedAtTouchdownKt,
            ["BrakeEnergyJoules"]        = s.BrakeEnergyJoules,
        };
}
