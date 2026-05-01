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
///   <item><c>Definitions.FlightData</c> — numeric SimVars polled at ~4 Hz (SIM_FRAME / interval
///       set to every 4th frame at 16 Hz ≈ 4 Hz).</item>
///   <item><c>Definitions.AircraftId</c> — string identity block (TITLE + ATC MODEL) polled once
///       per second, only on change.</item>
/// </list>
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

    // ── Data-definition and request enums ────────────────────────────────────

    private enum Definitions : uint { FlightData = 1, AircraftId = 2 }
    private enum Requests    : uint { FlightData = 1, AircraftId = 2 }
    private enum SysEvents   : uint { AircraftLoaded = 1 }

    // ── Flight-data struct (must match AddToDataDefinition order exactly) ────
    //
    // NOTE: Pack=1 and SequentialLayout are mandatory — SimConnect writes raw
    // bytes into this struct without any alignment padding.

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AircraftDataStruct
    {
        public double AirspeedIndicatedKt;     // AIRSPEED INDICATED             knots
        public double AirspeedTrueKt;          // AIRSPEED TRUE                  knots
        public double GroundVelocityKt;        // GROUND VELOCITY                knots
        public double AltitudeFt;              // INDICATED ALTITUDE             feet
        public double VerticalSpeedFpm;        // VERTICAL SPEED                 feet per minute
        public double HeadingMagnetic;         // PLANE HEADING DEGREES MAGNETIC degrees
        public double AmbientTemperatureC;     // AMBIENT TEMPERATURE            celsius
        public double EngineRpm;              // GENERAL ENG RPM:1              rpm
        public double EnginePctMaxRpm;        // GENERAL ENG PCT MAX RPM:1      percent  (maps to N1)
        public double TurbEngIttRankine;      // TURB ENG ITT:1                 rankine  (0 on piston)
        public double EngineTorqueFtLb;       // GENERAL ENG TORQUE:1           foot pounds
        public double FuelTotalWeightLb;      // FUEL TOTAL QUANTITY WEIGHT     pounds
        public double FuelFlowPph;            // ENG FUEL FLOW PPH:1            pounds per hour
        public double OilTemperatureC;        // GENERAL ENG OIL TEMPERATURE:1  celsius
        public double OilPressurePsi;         // GENERAL ENG OIL PRESSURE:1     psi
        public double SimOnGround;            // SIM ON GROUND                  bool (0/1 as double)
        public double GForce;                 // G FORCE                        g
        public double BrakeLeftPct;           // BRAKE LEFT POSITION            percent over 100
        public double BrakeRightPct;          // BRAKE RIGHT POSITION           percent over 100
    }

    // ── Aircraft-identity struct ─────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    private struct AircraftIdStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;      // TITLE           (max 256)

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AtcModel;   // ATC MODEL       (max 32)
    }

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

    public event EventHandler<SimConnectedEventArgs>?    Connected;
    public event EventHandler?                           Disconnected;
    public event EventHandler<SimErrorEventArgs>?        Error;
    public event EventHandler<AircraftIdentityEventArgs>?AircraftIdentityReceived;
    public event EventHandler<FlightSampleEventArgs>?    SampleProduced;

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
            if (!ok) break;         // WM_QUIT received
            if (msg.message == 0)  break;  // extra safety
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

        // Register numeric data block
        sc.AddToDataDefinition(Definitions.FlightData, "AIRSPEED INDICATED",             "knots",           SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "AIRSPEED TRUE",                  "knots",           SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "GROUND VELOCITY",                "knots",           SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "INDICATED ALTITUDE",             "feet",            SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "VERTICAL SPEED",                 "feet per minute", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "PLANE HEADING DEGREES MAGNETIC", "degrees",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "AMBIENT TEMPERATURE",            "celsius",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "GENERAL ENG RPM:1",              "rpm",             SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "GENERAL ENG PCT MAX RPM:1",      "percent",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "TURB ENG ITT:1",                 "rankine",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "GENERAL ENG TORQUE:1",           "foot pounds",     SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "FUEL TOTAL QUANTITY WEIGHT",     "pounds",          SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "ENG FUEL FLOW PPH:1",            "pounds per hour", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "GENERAL ENG OIL TEMPERATURE:1",  "celsius",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "GENERAL ENG OIL PRESSURE:1",     "psi",             SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "SIM ON GROUND",                  "bool",            SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "G FORCE",                        "gforce",          SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "BRAKE LEFT POSITION",            "percent",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.FlightData, "BRAKE RIGHT POSITION",           "percent",         SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);

        sc.RegisterDataDefineStruct<AircraftDataStruct>(Definitions.FlightData);

        // Poll every 4th visual frame — at 16 Hz target this gives ~4 Hz
        sc.RequestDataOnSimObject(
            Requests.FlightData,
            Definitions.FlightData,
            SimConnect.SIMCONNECT_OBJECT_ID_USER,
            SIMCONNECT_PERIOD.VISUAL_FRAME,
            SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
            origin:   0,
            interval: 3,   // 0 = every frame, 3 = every 4th frame
            limit:    0);

        // Register aircraft-identity string block
        sc.AddToDataDefinition(Definitions.AircraftId, "TITLE",     null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
        sc.AddToDataDefinition(Definitions.AircraftId, "ATC MODEL", null, SIMCONNECT_DATATYPE.STRING32,  0, SimConnect.SIMCONNECT_UNUSED);
        sc.RegisterDataDefineStruct<AircraftIdStruct>(Definitions.AircraftId);

        sc.RequestDataOnSimObject(
            Requests.AircraftId,
            Definitions.AircraftId,
            SimConnect.SIMCONNECT_OBJECT_ID_USER,
            SIMCONNECT_PERIOD.SECOND,
            SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
            0, 0, 0);

        // Subscribe to AircraftLoaded so we can re-request identity on aircraft change
        sc.SubscribeToSystemEvent(SysEvents.AircraftLoaded, "AircraftLoaded");

        Connected?.Invoke(this, new SimConnectedEventArgs { SimVersion = simVersion });
    }

    private void OnRecvQuit(SimConnect sc, SIMCONNECT_RECV data)
    {
        _logger.LogInformation("RealSimConnectClient: SimConnect session quit (MSFS exited).");
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecvException(SimConnect sc, SIMCONNECT_RECV_EXCEPTION data)
    {
        // Log but don't crash — many exceptions (e.g. UNKNOWN_SENDID during setup) are benign
        _logger.LogWarning(
            "RealSimConnectClient: SimConnect exception {Exception} sendId={SendId} index={Index}",
            data.dwException, data.dwSendID, data.dwIndex);

        Error?.Invoke(this, new SimErrorEventArgs
        {
            Message = $"SimConnect exception {data.dwException} (sendId={data.dwSendID})",
        });
    }

    private void OnRecvSimobjectData(SimConnect sc, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        if (data.dwRequestID == (uint)Requests.FlightData)
        {
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
                    Definitions.AircraftId,
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
        double avgBrakePct    = (raw.BrakeLeftPct + raw.BrakeRightPct) / 2.0 / 100.0;
        double gsNorm         = raw.GroundVelocityKt / 100.0;
        double brakeEnergyJ   = onGround && avgBrakePct > 0.01
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
