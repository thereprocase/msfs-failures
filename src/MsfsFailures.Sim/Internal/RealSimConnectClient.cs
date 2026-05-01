using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using MsfsFailures.Core.Wear;

namespace MsfsFailures.Sim.Internal;

/// <summary>
/// Production <see cref="ISimConnectClient"/> that wraps the MSFS
/// <c>Microsoft.FlightSimulator.SimConnect.SimConnect</c> managed library.
///
/// <para><b>Message pump</b>: SimConnect requires that <c>ReceiveMessage()</c> is called on the
/// same thread that created the <c>SimConnect</c> object and that the thread owns a Win32 message
/// loop.  We satisfy both requirements by spinning a dedicated <c>STA</c> background thread that:
/// <list type="number">
///   <item>Creates a hidden Win32 window handle via <c>CreateMessageWindow</c> (a message-only
///       HWND — no visible UI needed).</item>
///   <item>Opens the <c>SimConnect</c> session with that HWND and <c>WM_USER_SIMCONNECT</c>.</item>
///   <item>Runs a Win32 <c>GetMessage</c> / <c>TranslateMessage</c> / <c>DispatchMessage</c> loop
///       so the OS delivers <c>WM_USER_SIMCONNECT</c> notifications to our <c>WndProc</c>.</item>
/// </list>
/// This avoids any dependency on WPF <c>HwndSource</c> and keeps the Sim assembly free of a WPF
/// dependency.
/// </para>
///
/// <para><b>Data definitions</b>:
/// <list type="bullet">
///   <item>Flight-data block — numeric SimVars polled at ~4 Hz (VISUAL_FRAME / interval every
///       4th frame at 16 Hz ≈ 4 Hz).</item>
///   <item>AircraftId block — string identity block (TITLE + ATC MODEL) polled once per second,
///       only on change.</item>
/// </list>
/// </para>
///
/// <para><b>Self-healing subscription</b>: SimConnect validates SimVar names against the active
/// aircraft at <c>RequestDataOnSimObject</c> time (not at <c>AddToDataDefinition</c> time).  When
/// a var is unsupported (e.g. TURB ENG ITT:1 on the piston C172) SimConnect fires an async
/// exception-7 (NAME_UNRECOGNIZED / DATA_ERROR) and rejects the <em>entire</em> request — zero
/// samples flow.  We handle this by:
/// <list type="number">
///   <item>Recording the offending 1-based active-ordinal from <c>dwIndex</c>.</item>
///   <item>Mapping it back to the absolute SimVar slot via <see cref="_activeSlotMap"/>.</item>
///   <item>Adding the slot to <see cref="_skippedSlots"/>.</item>
///   <item>Calling <c>ClearDataDefinition</c>, incrementing the definition ID, and re-registering
///       all non-skipped vars.</item>
/// </list>
/// The struct always has all 19 fields; un-registered slots stay zero-initialised, which
/// <see cref="BuildSample"/> maps to -1 (N/A sentinel) for ITT and Torque — correct for
/// piston aircraft.  Rebuilds are capped at <see cref="MaxRebuildAttempts"/> = 5.
/// </para>
///
/// <para><b>Fallback</b>: if <c>SimConnect_Open</c> (constructor) throws a <c>COMException</c>
/// (MSFS not running, sandbox limit, etc.) <see cref="OpenAsync"/> catches it, logs a warning,
/// and re-throws so that <see cref="FallbackSimConnectClient"/> can transparently switch to the
/// mock path.</para>
/// </summary>
internal sealed class RealSimConnectClient : ISimConnectClient
{
    // ── SimConnect message ID ────────────────────────────────────────────────
    private const uint WM_USER_SIMCONNECT = 0x0402;

    // ── Win32 imports for message-only window ────────────────────────────────
    private const string WNDCLASS_NAME = "MsfsFailuresPump";
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint    cbSize;
        public uint    style;
        public IntPtr  lpfnWndProc;
        public int     cbClsExtra;
        public int     cbWndExtra;
        public IntPtr  hInstance;
        public IntPtr  hIcon;
        public IntPtr  hCursor;
        public IntPtr  hbrBackground;
        public string? lpszMenuName;
        public string  lpszClassName;
        public IntPtr  hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint   message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint   time;
        public int    ptX;
        public int    ptY;
    }

    // WM_QUIT = 0x0012 — posted to break the message loop during dispose
    private const uint WM_QUIT = 0x0012;

    // ── Request and system-event enums ───────────────────────────────────────

    private enum Requests  : uint { FlightData = 1, AircraftId = 2 }
    private enum SysEvents : uint { AircraftLoaded = 1 }

    // Internal enum for dynamic definition IDs.  We never use named members;
    // we cast uint → SimDefId directly.  This satisfies the SimConnect managed
    // API which requires an enum parameter.
    private enum SimDefId : uint { }

    // Fixed IDs for the two definition types:
    //   FlightData: starts at 100 and increments with each self-healing rebuild.
    //   AircraftId: fixed at 2.
    private const uint AircraftIdDefId    = 2;
    private const uint FlightDataDefIdBase = 100;

    // ── SimVar table ─────────────────────────────────────────────────────────
    //
    // 19 entries in struct order (slots 1-19, 1-based).  Index into this array
    // is the "absolute slot index" (0-based) used throughout the self-healing logic.

    private sealed record SimVarDef(string VarName, string Units, string DisplayName);

    private static readonly SimVarDef[] SimVars =
    {
        new("AIRSPEED INDICATED",             "knots",           "AIRSPEED INDICATED"),             // slot  1
        new("AIRSPEED TRUE",                  "knots",           "AIRSPEED TRUE"),                  // slot  2
        new("GROUND VELOCITY",                "knots",           "GROUND VELOCITY"),                // slot  3
        new("INDICATED ALTITUDE",             "feet",            "INDICATED ALTITUDE"),             // slot  4
        new("VERTICAL SPEED",                 "feet per minute", "VERTICAL SPEED"),                 // slot  5
        new("PLANE HEADING DEGREES MAGNETIC", "degrees",         "PLANE HEADING DEGREES MAGNETIC"), // slot  6
        new("AMBIENT TEMPERATURE",            "celsius",         "AMBIENT TEMPERATURE"),            // slot  7
        new("GENERAL ENG RPM:1",              "rpm",             "GENERAL ENG RPM:1"),              // slot  8
        new("GENERAL ENG PCT MAX RPM:1",      "percent",         "GENERAL ENG PCT MAX RPM:1"),      // slot  9
        new("TURB ENG ITT:1",                 "rankine",         "TURB ENG ITT:1 (turbine-only)"),  // slot 10
        new("GENERAL ENG TORQUE:1",           "foot pounds",     "GENERAL ENG TORQUE:1"),           // slot 11
        new("FUEL TOTAL QUANTITY WEIGHT",     "pounds",          "FUEL TOTAL QUANTITY WEIGHT"),     // slot 12
        new("ENG FUEL FLOW PPH:1",            "pounds per hour", "ENG FUEL FLOW PPH:1"),            // slot 13
        new("GENERAL ENG OIL TEMPERATURE:1",  "celsius",         "GENERAL ENG OIL TEMPERATURE:1"), // slot 14
        new("GENERAL ENG OIL PRESSURE:1",     "psi",             "GENERAL ENG OIL PRESSURE:1"),    // slot 15
        new("SIM ON GROUND",                  "bool",            "SIM ON GROUND"),                  // slot 16
        new("G FORCE",                        "gforce",          "G FORCE"),                        // slot 17
        new("BRAKE LEFT POSITION",            "percent",         "BRAKE LEFT POSITION"),            // slot 18
        new("BRAKE RIGHT POSITION",           "percent",         "BRAKE RIGHT POSITION"),           // slot 19
    };

    // ── Flight-data struct (fixed layout — all 19 doubles always present) ────
    //
    // Skipped vars simply don't get registered; their struct fields stay at 0.
    // BuildSample interprets 0 as -1 (N/A) for ITT and Torque (correct for piston).
    // Pack=1 + SequentialLayout is mandatory — SimConnect writes raw bytes.

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AircraftDataStruct
    {
        public double AirspeedIndicatedKt;     // slot  1
        public double AirspeedTrueKt;          // slot  2
        public double GroundVelocityKt;        // slot  3
        public double AltitudeFt;              // slot  4
        public double VerticalSpeedFpm;        // slot  5
        public double HeadingMagnetic;         // slot  6
        public double AmbientTemperatureC;     // slot  7
        public double EngineRpm;               // slot  8
        public double EnginePctMaxRpm;         // slot  9
        public double TurbEngIttRankine;       // slot 10
        public double EngineTorqueFtLb;        // slot 11
        public double FuelTotalWeightLb;       // slot 12
        public double FuelFlowPph;             // slot 13
        public double OilTemperatureC;         // slot 14
        public double OilPressurePsi;          // slot 15
        public double SimOnGround;             // slot 16
        public double GForce;                  // slot 17
        public double BrakeLeftPct;            // slot 18
        public double BrakeRightPct;           // slot 19
    }

    // ── Aircraft-identity struct ─────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    private struct AircraftIdStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;      // TITLE     (max 256)

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AtcModel;   // ATC MODEL (max 32)
    }

    // ── Self-healing subscription state ──────────────────────────────────────
    //
    // All fields below are accessed only on the SimConnect pump thread — no locking.
    //
    // _skippedSlots: set of 0-based SimVars[] indices blacklisted for this aircraft.
    //   These are NOT passed to AddToDataDefinition; their struct fields stay zero.
    //
    // _activeSlotMap: after each RegisterFlightDataDefinition, maps
    //   active-call ordinal (0-based) → absolute SimVars[] index (0-based).
    //   dwIndex in exception 7 is 1-based active ordinal; subtract 1 to index here.
    //
    // _flightDataDefId: current definition ID uint (starts at FlightDataDefIdBase,
    //   incremented on each rebuild to avoid reusing a stale ID).
    //
    // _rebuildCount: total rebuilds so far; capped at MaxRebuildAttempts.
    //
    // _subscriptionStable: set on first successful data sample, used for the
    //   "subscription stable; receiving samples" log message.

    private const int MaxRebuildAttempts = 5;

    private readonly HashSet<int> _skippedSlots  = new();
    private readonly List<int>    _activeSlotMap  = new(19);
    private uint _flightDataDefId   = FlightDataDefIdBase;
    private int  _rebuildCount      = 0;
    private bool _subscriptionStable = false;

    // ── Fields ───────────────────────────────────────────────────────────────

    private readonly ILogger<RealSimConnectClient> _logger;

    private SimConnect?  _sc;
    private Thread?      _pumpThread;
    private IntPtr       _hwnd = IntPtr.Zero;

    // Guard against re-entrant disposal
    private volatile bool _disposed;

    // Touchdown detection
    private bool   _lastOnGround;
    private bool   _groundInitialized;

    // ── ISimConnectClient events ─────────────────────────────────────────────

    public event EventHandler<SimConnectedEventArgs>?     Connected;
    public event EventHandler?                            Disconnected;
    public event EventHandler<SimErrorEventArgs>?         Error;
    public event EventHandler<AircraftIdentityEventArgs>? AircraftIdentityReceived;
    public event EventHandler<FlightSampleEventArgs>?     SampleProduced;

    // ── Constructor ──────────────────────────────────────────────────────────

    public RealSimConnectClient(ILogger<RealSimConnectClient> logger)
    {
        _logger = logger;
    }

    // ── ISimConnectClient ────────────────────────────────────────────────────

    /// <summary>
    /// Starts the Win32 message-pump thread and opens the SimConnect session.
    /// Throws <see cref="InvalidOperationException"/> if already connected.
    /// Throws <see cref="COMException"/> if MSFS is not running (caller should catch + fall back).
    /// </summary>
    public Task OpenAsync(CancellationToken ct = default)
    {
        if (_sc is not null)
            throw new InvalidOperationException("RealSimConnectClient is already connected.");

        // All SimConnect work must happen on the same STA thread that owns the HWND.
        // We signal the calling thread via a TaskCompletionSource once the constructor
        // either succeeds or throws.
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pumpThread = new Thread(() => PumpThreadMain(tcs))
        {
            Name         = "SimConnect-Pump",
            IsBackground = true,
        };
        _pumpThread.SetApartmentState(ApartmentState.STA);
        _pumpThread.Start();

        return tcs.Task;
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        Dispose(disposing: true);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Dispose(disposing: true);
        return ValueTask.CompletedTask;
    }

    // ── Message-pump thread ───────────────────────────────────────────────────

    private void PumpThreadMain(TaskCompletionSource<bool> tcs)
    {
        try
        {
            // 1. Create a message-only HWND on this thread
            _hwnd = CreateMessageOnlyWindow();

            // 2. Open SimConnect — throws COMException if MSFS not running
            _sc = new SimConnect("msfs-failures", _hwnd, WM_USER_SIMCONNECT, null, 0);

            // 3. Wire events
            _sc.OnRecvOpen            += OnRecvOpen;
            _sc.OnRecvQuit            += OnRecvQuit;
            _sc.OnRecvException       += OnRecvException;
            _sc.OnRecvSimobjectData   += OnRecvSimobjectData;
            _sc.OnRecvEventFilename   += OnRecvEventFilename;

            // 4. Signal caller — success
            tcs.SetResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "RealSimConnectClient: SimConnect_Open failed ({Message}) — falling back to mock",
                ex.Message);
            tcs.SetException(ex);
            return;
        }

        // 5. Win32 message loop
        while (!_disposed)
        {
            bool ok = GetMessage(out MSG msg, IntPtr.Zero, 0, 0);
            if (!ok) break;        // WM_QUIT received
            if (msg.message == 0) break;  // extra safety
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        // 6. Cleanup on this thread
        CleanupSimConnect();
    }

    // ── Win32 HWND ───────────────────────────────────────────────────────────

    private IntPtr CreateMessageOnlyWindow()
    {
        var hInst = GetModuleHandle(null);

        // WndProc delegate — must be kept alive for the lifetime of the window
        _wndProc = WndProc;

        var wc = new WNDCLASSEX
        {
            cbSize        = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc   = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance     = hInst,
            lpszClassName = WNDCLASS_NAME + Thread.CurrentThread.ManagedThreadId,
        };

        ushort atom = RegisterClassEx(ref wc);
        if (atom == 0)
        {
            // Class may already be registered if we're re-creating — not fatal
        }

        var hwnd = CreateWindowEx(
            0,
            wc.lpszClassName,
            "MsfsFailuresPump",
            0, 0, 0, 0, 0,
            HWND_MESSAGE,
            IntPtr.Zero,
            hInst,
            IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create message-only window for SimConnect pump.");

        return hwnd;
    }

    // Delegate kept alive to prevent GC collection while window exists
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate? _wndProc;

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_USER_SIMCONNECT)
        {
            try { _sc?.ReceiveMessage(); }
            catch (Exception ex) when (!_disposed)
            {
                _logger.LogWarning(ex, "RealSimConnectClient: exception in ReceiveMessage.");
            }
            return IntPtr.Zero;
        }

        // Default window proc for all other messages
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    // ── SimConnect event handlers (called on pump thread) ────────────────────

    private void OnRecvOpen(SimConnect sc, SIMCONNECT_RECV_OPEN data)
    {
        string simVersion =
            $"{data.dwApplicationVersionMajor}.{data.dwApplicationVersionMinor}" +
            $" build {data.dwApplicationBuildMajor}.{data.dwApplicationBuildMinor}";

        _logger.LogInformation(
            "RealSimConnectClient: SimConnect session opened. SimVersion={SimVersion}", simVersion);

        // Reset self-healing state for this connection
        _skippedSlots.Clear();
        _activeSlotMap.Clear();
        _flightDataDefId    = FlightDataDefIdBase;
        _rebuildCount       = 0;
        _subscriptionStable = false;

        // Register the self-healing flight-data subscription
        RegisterFlightDataDefinition(sc);

        // Register aircraft-identity string block (fixed def ID)
        var idDef = (SimDefId)AircraftIdDefId;
        sc.AddToDataDefinition(idDef, "TITLE",     null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(idDef, "ATC MODEL", null, SIMCONNECT_DATATYPE.STRING32,  0, SimConnect.SIMCONNECT_UNUSED);
        sc.RegisterDataDefineStruct<AircraftIdStruct>(idDef);

        sc.RequestDataOnSimObject(
            Requests.AircraftId,
            idDef,
            SimConnect.SIMCONNECT_OBJECT_ID_USER,
            SIMCONNECT_PERIOD.SECOND,
            SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
            0, 0, 0);

        // Subscribe to AircraftLoaded so we can re-request identity on aircraft change
        sc.SubscribeToSystemEvent(SysEvents.AircraftLoaded, "AircraftLoaded");

        Connected?.Invoke(this, new SimConnectedEventArgs { SimVersion = simVersion });
    }

    // ── Self-healing subscription helpers ────────────────────────────────────

    /// <summary>
    /// Registers the flight-data data definition (with all non-skipped vars), the
    /// struct layout, and issues <c>RequestDataOnSimObject</c> for the current
    /// <see cref="_flightDataDefId"/>.  Also rebuilds <see cref="_activeSlotMap"/>.
    /// </summary>
    private void RegisterFlightDataDefinition(SimConnect sc)
    {
        var defId = (SimDefId)_flightDataDefId;

        _activeSlotMap.Clear();

        for (int i = 0; i < SimVars.Length; i++)
        {
            if (_skippedSlots.Contains(i)) continue;

            sc.AddToDataDefinition(
                defId,
                SimVars[i].VarName,
                SimVars[i].Units,
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            // Track: active-call ordinal (0-based) → absolute slot index (0-based)
            _activeSlotMap.Add(i);
        }

        sc.RegisterDataDefineStruct<AircraftDataStruct>(defId);

        // Poll every 4th visual frame — at 16 Hz target this gives ~4 Hz
        sc.RequestDataOnSimObject(
            Requests.FlightData,
            defId,
            SimConnect.SIMCONNECT_OBJECT_ID_USER,
            SIMCONNECT_PERIOD.VISUAL_FRAME,
            SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
            origin:   0,
            interval: 3,   // 0 = every frame, 3 = every 4th frame
            limit:    0);

        int skipped = _skippedSlots.Count;
        if (skipped == 0)
            _logger.LogInformation(
                "RealSimConnectClient: flight-data subscription started ({Count} vars, defId={DefId}).",
                SimVars.Length, _flightDataDefId);
        else
            _logger.LogInformation(
                "RealSimConnectClient: flight-data subscription rebuilt " +
                "({Active} vars active, {Skipped} skipped, defId={DefId}).",
                SimVars.Length - skipped, skipped, _flightDataDefId);
    }

    /// <summary>
    /// Tears down the current definition, marks <paramref name="absoluteSlotIndex"/> as
    /// skipped, increments the definition ID, and re-registers.
    /// </summary>
    private void RebuildWithSkippedSlot(SimConnect sc, int absoluteSlotIndex)
    {
        if (_rebuildCount >= MaxRebuildAttempts)
        {
            _logger.LogError(
                "RealSimConnectClient: reached max rebuild attempts ({Max}). " +
                "Samples will NOT flow. Check SimVar compatibility for this aircraft.",
                MaxRebuildAttempts);
            return;
        }

        string varDisplay = absoluteSlotIndex < SimVars.Length
            ? SimVars[absoluteSlotIndex].DisplayName
            : $"slot#{absoluteSlotIndex + 1}";

        _skippedSlots.Add(absoluteSlotIndex);
        _rebuildCount++;

        _logger.LogWarning(
            "RealSimConnectClient: rebuilding data definition without slot {Slot} ({VarName}). " +
            "Attempt {Attempt}/{Max}.",
            absoluteSlotIndex + 1, varDisplay, _rebuildCount, MaxRebuildAttempts);

        try { sc.ClearDataDefinition((SimDefId)_flightDataDefId); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RealSimConnectClient: ClearDataDefinition failed (non-fatal).");
        }

        _flightDataDefId++;  // always use a fresh ID after clearing
        RegisterFlightDataDefinition(sc);
    }

    private void OnRecvQuit(SimConnect sc, SIMCONNECT_RECV data)
    {
        _logger.LogInformation("RealSimConnectClient: SimConnect session quit (MSFS exited).");
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecvException(SimConnect sc, SIMCONNECT_RECV_EXCEPTION data)
    {
        const uint ExcDataError = 7;   // SIMCONNECT_EXCEPTION_DATA_ERROR / NAME_UNRECOGNIZED

        if (data.dwException == ExcDataError && data.dwIndex != 0xFFFFFFFF)
        {
            // dwIndex is the 1-based ordinal of the failing AddToDataDefinition call
            // among the *active* (non-skipped) vars in the current definition.
            int activeIdx = (int)data.dwIndex - 1;  // convert to 0-based

            if (activeIdx >= 0 && activeIdx < _activeSlotMap.Count)
            {
                int absoluteSlot = _activeSlotMap[activeIdx];
                _logger.LogWarning(
                    "RealSimConnectClient: SimConnect exception 7 (NAME_UNRECOGNIZED) " +
                    "sendId={SendId} activeOrdinal={Ordinal} → absolute slot {Slot} ({VarName}). " +
                    "Triggering self-healing rebuild.",
                    data.dwSendID, data.dwIndex, absoluteSlot + 1,
                    SimVars[absoluteSlot].DisplayName);

                RebuildWithSkippedSlot(sc, absoluteSlot);
            }
            else
            {
                // dwIndex does not map into our active var list (could be a different request).
                // As a conservative fallback, skip both turbine-only vars if not already skipped.
                _logger.LogWarning(
                    "RealSimConnectClient: SimConnect exception 7 sendId={SendId} index={Index} " +
                    "— index out of range (activeMap.Count={Count}). " +
                    "Fallback: skipping TURB ENG ITT:1 (slot 10) and GENERAL ENG TORQUE:1 (slot 11).",
                    data.dwSendID, data.dwIndex, _activeSlotMap.Count);

                // 0-based indices: ITT=9, Torque=10
                bool changed = _skippedSlots.Add(9) | _skippedSlots.Add(10);
                if (changed && _rebuildCount < MaxRebuildAttempts)
                {
                    _rebuildCount++;
                    try { sc.ClearDataDefinition((SimDefId)_flightDataDefId); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "RealSimConnectClient: ClearDataDefinition failed (non-fatal).");
                    }
                    _flightDataDefId++;
                    RegisterFlightDataDefinition(sc);
                }
            }

            Error?.Invoke(this, new SimErrorEventArgs
            {
                Message = $"SimConnect exception 7 (sendId={data.dwSendID}): unsupported SimVar — rebuilding subscription.",
            });
            return;
        }

        // All other exceptions: log and continue — session remains active.
        string indexInfo = data.dwIndex == 0xFFFFFFFF
            ? "(index N/A)"
            : $"index={data.dwIndex}";

        _logger.LogWarning(
            "RealSimConnectClient: SimConnect exception {Exception} sendId={SendId} {IndexInfo}.",
            data.dwException, data.dwSendID, indexInfo);

        Error?.Invoke(this, new SimErrorEventArgs
        {
            Message = $"SimConnect exception {data.dwException} (sendId={data.dwSendID})",
        });
    }

    private void OnRecvSimobjectData(SimConnect sc, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        if (data.dwRequestID == (uint)Requests.FlightData)
        {
            if (!_subscriptionStable)
            {
                _subscriptionStable = true;
                _logger.LogInformation(
                    "RealSimConnectClient: subscription stable; receiving samples. " +
                    "({Skipped} var(s) skipped for this aircraft.)",
                    _skippedSlots.Count);
            }

            var raw = (AircraftDataStruct)data.dwData[0];
            var sample = BuildSample(raw);
            SampleProduced?.Invoke(this, new FlightSampleEventArgs { Sample = sample });
        }
        else if (data.dwRequestID == (uint)Requests.AircraftId)
        {
            var id = (AircraftIdStruct)data.dwData[0];
            _logger.LogInformation(
                "RealSimConnectClient: Aircraft identity — Title={Title} AtcModel={AtcModel}",
                id.Title, id.AtcModel);
            AircraftIdentityReceived?.Invoke(this, new AircraftIdentityEventArgs
            {
                AircraftTitle = id.Title,
                AtcModel      = id.AtcModel,
            });
        }
    }

    private void OnRecvEventFilename(SimConnect sc, SIMCONNECT_RECV_EVENT_FILENAME data)
    {
        if (data.uEventID == (uint)SysEvents.AircraftLoaded)
        {
            _logger.LogInformation(
                "RealSimConnectClient: AircraftLoaded event — re-requesting identity. File={File}",
                data.szFileName);

            // Re-request the identity block once (one-shot) after an aircraft load
            try
            {
                sc.RequestDataOnSimObject(
                    Requests.AircraftId,
                    (SimDefId)AircraftIdDefId,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                    0, 0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RealSimConnectClient: failed to re-request identity after AircraftLoaded.");
            }
        }
    }

    // ── Sample builder ────────────────────────────────────────────────────────

    private FlightTickSample BuildSample(AircraftDataStruct raw)
    {
        bool onGround = raw.SimOnGround > 0.5;

        // Touchdown detection: false→true transition
        double gsAtTouchdown = 0;
        if (_groundInitialized && !_lastOnGround && onGround)
            gsAtTouchdown = raw.GroundVelocityKt;

        _lastOnGround      = onGround;
        _groundInitialized = true;

        // ITT: TURB ENG ITT:1 is in Rankine; convert to °C.
        // For the Cessna 172 (piston) this will be 0 — FlightTickSample.IttC accepts -1 for N/A.
        double ittC = raw.TurbEngIttRankine > 0
            ? raw.TurbEngIttRankine * (5.0 / 9.0) - 273.15
            : -1.0;

        // Brake energy (coarse approximation, on-ground only):
        //   E ≈ 0.5 * (BrakeL + BrakeR) * (GS/100)² * 50000  [J per tick]
        // Rationale: we don't have mass; 50000 is a calibration constant chosen so that a
        // typical C172 landing rollout (GS ~55 kt, both brakes ~70%) yields ~10-15 kJ/tick,
        // which is in the right order of magnitude for light-aircraft brake energy.
        double avgBrakePct  = (raw.BrakeLeftPct + raw.BrakeRightPct) / 2.0 / 100.0;
        double gsNorm       = raw.GroundVelocityKt / 100.0;
        double brakeEnergyJ = onGround && avgBrakePct > 0.01
            ? 0.5 * (raw.BrakeLeftPct + raw.BrakeRightPct) / 100.0 * gsNorm * gsNorm * 50_000.0
            : 0.0;

        return new FlightTickSample(
            Timestamp:                DateTimeOffset.UtcNow,
            OnGround:                 onGround,
            IasKt:                    raw.AirspeedIndicatedKt,
            GsKt:                     raw.GroundVelocityKt,
            VerticalSpeedFpm:         raw.VerticalSpeedFpm,
            GLoad:                    raw.GForce,
            OatC:                     raw.AmbientTemperatureC,
            EngineRpm:                raw.EngineRpm,
            EngineN1Pct:              raw.EnginePctMaxRpm,
            IttC:                     ittC,
            TorqueFtLb:               raw.EngineTorqueFtLb > 0 ? raw.EngineTorqueFtLb : -1.0,
            FuelFlowPph:              raw.FuelFlowPph,
            OilTempC:                 raw.OilTemperatureC,
            OilPressurePsi:           raw.OilPressurePsi,
            GroundspeedAtTouchdownKt: gsAtTouchdown,
            BrakeEnergyJoules:        brakeEnergyJ);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        // Post WM_QUIT to break the message loop on the pump thread
        if (_hwnd != IntPtr.Zero)
            PostMessage(_hwnd, WM_QUIT, IntPtr.Zero, IntPtr.Zero);

        // Wait for pump thread to exit (max 3 s)
        _pumpThread?.Join(TimeSpan.FromSeconds(3));
    }

    private void CleanupSimConnect()
    {
        try { _sc?.Dispose(); } catch { /* ignored */ }
        _sc = null;

        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }
}
