using MsfsFailures.Core.Wear;

namespace MsfsFailures.Sim.Internal;

// ── Synthetic flight-profile constants ────────────────────────────────────────
//
//  All durations are wall-clock seconds.  The default cycle is compressed to
//  ~360 s ≈ 6 minutes total for fast dev iteration.  Tests can inject a
//  DurationScale < 1.0 to run the cycle faster.
//
//  PT6A-114A (Cessna 208B Caravan) reference values used for targets:
//    ITT cruise  685 °C   ITT climb  710 °C   ITT takeoff 720 °C
//    Torque cruise 1410 ft-lb   Torque climb 1700 ft-lb
//    Oil temp cruise 79 °C   Oil press cruise 87 psi
//    N1 cruise ~96 %     Fuel flow cruise ~370 lb/h
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Pure state machine that produces <see cref="FlightTickSample"/> values for a
/// synthetic Cessna 208B (PT6A-114A) flight profile.
///
/// Thread-safety: NOT thread-safe — callers are responsible for serialisation.
/// The <see cref="MockSimConnectClient"/> timer callback is serialised by the fact
/// that a <see cref="System.Threading.Timer"/> callback is not re-entrant.
/// </summary>
internal sealed class SyntheticFlightProfile
{
    // ── Phase durations (seconds, at DurationScale = 1.0) ────────────────────
    public const double TaxiOutDurationBase  =  75;   // 60–90 s mid-point
    public const double TakeoffDurationBase  =  30;
    public const double ClimbDurationBase    = 150;   // 120–180 s mid-point
    public const double CruiseDurationBase   = 750;   // 600–900 s mid-point (dominates cycle)
    public const double DescentDurationBase  = 150;
    public const double ApproachDurationBase =  75;   // 60–90 s mid-point
    public const double LandingDurationBase  =   8;   // 5–10 s mid-point
    public const double TaxiInDurationBase   =  60;

    /// <summary>Total nominal cycle duration in seconds at scale 1.0 (≈ 6 min).</summary>
    public static double NominalCycleDurationSec =>
        TaxiOutDurationBase + TakeoffDurationBase + ClimbDurationBase + CruiseDurationBase
        + DescentDurationBase + ApproachDurationBase + LandingDurationBase + TaxiInDurationBase;

    // ── Noise fraction ────────────────────────────────────────────────────────
    private const double NoiseFraction = 0.02;   // ±2 %

    // ── Target values (PT6A-114A Caravan) ────────────────────────────────────
    private const double OatCruise          =  15.0;
    private const double OilTempTaxiStart   =  30.0;
    private const double OilTempTaxiWarm    =  70.0;
    private const double OilTempCruise      =  79.0;
    private const double OilTempTaxiIn      =  80.0;
    private const double OilPressCruise     =  87.0;
    private const double OilPressIdle       =  82.0;
    private const double RpmIdle            = 700.0;
    private const double RpmTakeoff         = 2200.0;
    private const double N1Idle             =  52.0;
    private const double N1Cruise           =  96.0;
    private const double N1Climb            = 100.0;
    private const double N1Takeoff          = 101.5;
    private const double IttIdle            = 450.0;
    private const double IttTakeoff         = 720.0;
    private const double IttClimb          = 710.0;
    private const double IttCruise         = 685.0;
    private const double IttDescent        = 650.0;
    private const double IttApproach       = 610.0;
    private const double TorqueTakeoff     = 1800.0;
    private const double TorqueClimb       = 1700.0;
    private const double TorqueCruise      = 1410.0;
    private const double TorqueDescent     =  900.0;
    private const double TorqueApproach    =  600.0;
    private const double TorqueIdle        =  150.0;
    private const double FuelFlowCruise    = 370.0;
    private const double FuelFlowClimb     = 450.0;
    private const double FuelFlowIdle      =  90.0;
    private const double IasTakeoffSpeed   =  85.0;
    private const double IasClimb         = 110.0;
    private const double IasCruise        = 168.0;
    private const double GsCruise         = 174.0;
    private const double IasDescent       = 150.0;
    private const double IasApproach      =  95.0;
    private const double IasLandingRollout =  60.0;
    private const double IasTaxi          =   8.0;
    private const double VsiClimb         = 700.0;
    private const double VsiDescent       = -500.0;
    private const double VsiTouchdown     = -300.0;
    private const double BrakeEnergyLanding = 30_000.0; // J per tick during rollout (~5 ticks)

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly Random _rng;

    /// <summary>Scale applied to all phase durations. 1.0 = ~6 min cycle.</summary>
    private readonly double _durationScale;

    /// <summary>Current flight phase.</summary>
    public FlightPhase CurrentPhase { get; private set; } = FlightPhase.TaxiOut;

    private double _phaseElapsedSec;
    private double _oilTemp = OilTempTaxiStart;
    private bool   _touchdownThisCycle;
    private int    _landingRolloutTicksRemaining;

    public SyntheticFlightProfile(Random rng, double durationScale = 1.0)
    {
        _rng           = rng;
        _durationScale = durationScale;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Advance the profile by <paramref name="elapsedSec"/> seconds and produce the
    /// next <see cref="FlightTickSample"/>.
    /// </summary>
    public FlightTickSample Tick(double elapsedSec)
    {
        var sample = BuildSample(elapsedSec);

        _phaseElapsedSec += elapsedSec;
        AdvancePhaseIfDue();

        return sample;
    }

    // ── Phase advancement ─────────────────────────────────────────────────────

    private void AdvancePhaseIfDue()
    {
        double duration = PhaseDuration(CurrentPhase);
        if (_phaseElapsedSec < duration) return;

        var next = CurrentPhase switch
        {
            FlightPhase.TaxiOut  => FlightPhase.Takeoff,
            FlightPhase.Takeoff  => FlightPhase.Climb,
            FlightPhase.Climb    => FlightPhase.Cruise,
            FlightPhase.Cruise   => FlightPhase.Descent,
            FlightPhase.Descent  => FlightPhase.Approach,
            FlightPhase.Approach => FlightPhase.Landing,
            FlightPhase.Landing  => FlightPhase.TaxiIn,
            FlightPhase.TaxiIn   => FlightPhase.TaxiOut,
            _                    => FlightPhase.TaxiOut,
        };

        CurrentPhase     = next;
        _phaseElapsedSec = 0;

        if (CurrentPhase == FlightPhase.TaxiOut)
            _touchdownThisCycle = false;

        if (CurrentPhase == FlightPhase.Landing)
            _landingRolloutTicksRemaining = 5;
    }

    // ── Sample builder ────────────────────────────────────────────────────────

    private FlightTickSample BuildSample(double tickSec)
    {
        double t = _phaseElapsedSec / Math.Max(0.001, PhaseDuration(CurrentPhase));

        bool   onGround;
        double ias, gs, vsi, gLoad;
        double rpm, n1, itt, torque, fuelFlow, oilPress;
        double gsAtTouchdown = 0;
        double brakeEnergy   = 0;

        switch (CurrentPhase)
        {
            case FlightPhase.TaxiOut:
                onGround = true;
                ias      = 0;
                gs       = Lerp(0, IasTaxi, t);
                vsi      = 0;
                gLoad    = 1.0;
                rpm      = RpmIdle;
                n1       = N1Idle;
                itt      = IttIdle;
                torque   = TorqueIdle;
                fuelFlow = FuelFlowIdle;
                _oilTemp = Lerp(OilTempTaxiStart, OilTempTaxiWarm, t);
                oilPress = OilPressIdle;
                break;

            case FlightPhase.Takeoff:
                onGround = t < 0.6;
                ias      = Lerp(0, IasTakeoffSpeed * 1.1, t);
                gs       = ias;
                vsi      = t < 0.6 ? 0 : Lerp(0, VsiClimb * 0.5, (t - 0.6) / 0.4);
                gLoad    = t < 0.6 ? 1.0 : Lerp(1.0, 1.05, (t - 0.6) / 0.4);
                rpm      = RpmTakeoff;
                n1       = N1Takeoff;
                itt      = IttTakeoff;
                torque   = TorqueTakeoff;
                fuelFlow = FuelFlowClimb * 1.1;
                _oilTemp = OilTempTaxiWarm;
                oilPress = OilPressCruise;
                break;

            case FlightPhase.Climb:
                onGround = false;
                ias      = Lerp(IasTakeoffSpeed * 1.1, IasClimb, t);
                gs       = ias * 1.03;
                vsi      = VsiClimb;
                gLoad    = 1.0;
                rpm      = RpmTakeoff;
                n1       = N1Climb;
                itt      = IttClimb;
                torque   = TorqueClimb;
                fuelFlow = FuelFlowClimb;
                _oilTemp = Lerp(OilTempTaxiWarm, OilTempCruise, t);
                oilPress = OilPressCruise;
                break;

            case FlightPhase.Cruise:
                onGround = false;
                ias      = IasCruise;
                gs       = GsCruise;
                vsi      = 0;
                gLoad    = 1.0;
                rpm      = RpmTakeoff * 0.97;
                n1       = N1Cruise;
                itt      = IttCruise;
                torque   = TorqueCruise;
                fuelFlow = FuelFlowCruise;
                _oilTemp = OilTempCruise;
                oilPress = OilPressCruise;
                break;

            case FlightPhase.Descent:
                onGround = false;
                ias      = Lerp(IasCruise, IasDescent, t);
                gs       = ias * 1.03;
                vsi      = VsiDescent;
                gLoad    = 1.0;
                rpm      = RpmTakeoff * 0.92;
                n1       = Lerp(N1Cruise, N1Idle * 1.4, t);
                itt      = Lerp(IttCruise, IttDescent, t);
                torque   = Lerp(TorqueCruise, TorqueDescent, t);
                fuelFlow = Lerp(FuelFlowCruise, FuelFlowIdle * 2, t);
                _oilTemp = Lerp(OilTempCruise, OilTempTaxiIn, t);
                oilPress = Lerp(OilPressCruise, OilPressIdle, t);
                break;

            case FlightPhase.Approach:
                onGround = false;
                ias      = Lerp(IasDescent, IasApproach, t);
                gs       = ias;
                vsi      = Lerp(VsiDescent, -200, t);
                gLoad    = 1.0;
                rpm      = RpmTakeoff * 0.85;
                n1       = Lerp(N1Idle * 1.4, N1Idle * 1.2, t);
                itt      = Lerp(IttDescent, IttApproach, t);
                torque   = Lerp(TorqueDescent, TorqueApproach, t);
                fuelFlow = Lerp(FuelFlowIdle * 2, FuelFlowIdle * 1.5, t);
                _oilTemp = OilTempTaxiIn;
                oilPress = OilPressIdle;
                break;

            case FlightPhase.Landing:
                // First tick in the phase = touchdown.
                bool isTouchdownTick = _phaseElapsedSec < tickSec * 1.5;
                if (isTouchdownTick && !_touchdownThisCycle)
                {
                    gsAtTouchdown       = IasLandingRollout;
                    _touchdownThisCycle = true;
                }
                onGround = true;
                ias      = Lerp(IasLandingRollout, 0, t);
                gs       = ias;
                vsi      = Lerp(VsiTouchdown, 0, t);
                gLoad    = 1.0 + (1 - t) * 0.3;
                rpm      = Lerp(RpmTakeoff * 0.85, RpmIdle, t);
                n1       = Lerp(N1Idle * 1.2, N1Idle, t);
                itt      = Lerp(IttApproach, IttIdle, t);
                torque   = Lerp(TorqueApproach, TorqueIdle, t);
                fuelFlow = Lerp(FuelFlowIdle * 1.5, FuelFlowIdle, t);
                _oilTemp = OilTempTaxiIn;
                oilPress = OilPressIdle;
                if (_landingRolloutTicksRemaining > 0)
                {
                    brakeEnergy = BrakeEnergyLanding;
                    _landingRolloutTicksRemaining--;
                }
                break;

            case FlightPhase.TaxiIn:
            default:
                onGround = true;
                ias      = 0;
                gs       = Lerp(IasTaxi, 0, t);
                vsi      = 0;
                gLoad    = 1.0;
                rpm      = RpmIdle;
                n1       = N1Idle;
                itt      = IttIdle;
                torque   = TorqueIdle;
                fuelFlow = FuelFlowIdle;
                _oilTemp = OilTempTaxiIn;
                oilPress = OilPressIdle;
                break;
        }

        return new FlightTickSample(
            Timestamp:                DateTimeOffset.UtcNow,
            OnGround:                 onGround,
            IasKt:                    Jitter(ias),
            GsKt:                     Jitter(gs),
            VerticalSpeedFpm:         Jitter(vsi),
            GLoad:                    gLoad + SmallNoise(0.02),
            OatC:                     OatCruise + SmallNoise(1),
            EngineRpm:                Jitter(rpm),
            EngineN1Pct:              Jitter(n1),
            IttC:                     Jitter(itt),
            TorqueFtLb:               Jitter(torque),
            FuelFlowPph:              Jitter(fuelFlow),
            OilTempC:                 Jitter(_oilTemp),
            OilPressurePsi:           Jitter(oilPress),
            GroundspeedAtTouchdownKt: gsAtTouchdown,
            BrakeEnergyJoules:        brakeEnergy);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private double PhaseDuration(FlightPhase phase) => _durationScale * (phase switch
    {
        FlightPhase.TaxiOut  => TaxiOutDurationBase,
        FlightPhase.Takeoff  => TakeoffDurationBase,
        FlightPhase.Climb    => ClimbDurationBase,
        FlightPhase.Cruise   => CruiseDurationBase,
        FlightPhase.Descent  => DescentDurationBase,
        FlightPhase.Approach => ApproachDurationBase,
        FlightPhase.Landing  => LandingDurationBase,
        FlightPhase.TaxiIn   => TaxiInDurationBase,
        _                    => 60,
    });

    private static double Lerp(double a, double b, double t) => a + (b - a) * Math.Clamp(t, 0, 1);

    private double Jitter(double value)
    {
        if (value == 0) return 0;
        return value * (1 + (_rng.NextDouble() * 2 - 1) * NoiseFraction);
    }

    private double SmallNoise(double range) => (_rng.NextDouble() * 2 - 1) * range;
}

/// <summary>Flight phases in the synthetic profile.</summary>
internal enum FlightPhase
{
    TaxiOut, Takeoff, Climb, Cruise, Descent, Approach, Landing, TaxiIn
}
