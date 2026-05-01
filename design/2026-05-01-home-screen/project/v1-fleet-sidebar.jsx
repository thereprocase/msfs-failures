// Variant 1 — Fleet-first with left sidebar.
// Calm, balanced. Mono accents on numbers/IDs only.
// Click a fleet card → opens detail drawer. Toggles between Fleet/Squawks/Maintenance.

const v1Pal = {
  bg: '#16140f',          // warm near-black
  panel: '#1d1a14',
  panel2: '#22201a',
  border: 'rgba(255,255,255,.06)',
  borderStrong: 'rgba(255,255,255,.10)',
  text: '#e8e3d6',
  dim: '#9a9080',
  faint: '#5e574b',
  accent: '#d4a24c',      // amber
  green: '#7fb069',
  amber: '#d4a24c',
  orange: '#d97757',
  red: '#c25450',
  mono: '"JetBrains Mono", "SF Mono", Consolas, monospace',
};

const Pill = ({ children, color, bg }) => (
  <span style={{
    display: 'inline-flex', alignItems: 'center', gap: 6,
    padding: '2px 8px', borderRadius: 3,
    fontFamily: v1Pal.mono, fontSize: 10, fontWeight: 600,
    letterSpacing: '0.06em',
    color: color || v1Pal.text,
    background: bg || 'rgba(255,255,255,.04)',
    border: '1px solid ' + (color ? color + '44' : 'rgba(255,255,255,.08)'),
    textTransform: 'uppercase',
  }}>{children}</span>
);

const StatusDot = ({ color, pulse }) => (
  <span style={{
    width: 7, height: 7, borderRadius: '50%',
    background: color, flex: '0 0 7px',
    boxShadow: pulse ? `0 0 0 0 ${color}` : 'none',
    animation: pulse ? 'v1pulse 2s ease-out infinite' : 'none',
  }}/>
);

const WearBar = ({ value, label, low }) => {
  const pct = Math.round(value * 100);
  const color = value < 0.25 ? v1Pal.red : value < 0.5 ? v1Pal.orange : value < 0.75 ? v1Pal.amber : v1Pal.green;
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 6 }}>
        <span style={{ fontSize: 9, letterSpacing: '0.1em', color: v1Pal.faint, textTransform: 'uppercase', fontFamily: v1Pal.mono }}>{label}</span>
        <span style={{ fontSize: 10, color: low ? color : v1Pal.dim, fontFamily: v1Pal.mono, fontWeight: 600 }}>{pct}%</span>
      </div>
      <div style={{ height: 3, background: 'rgba(255,255,255,.05)', borderRadius: 1, overflow: 'hidden' }}>
        <div style={{ width: pct + '%', height: '100%', background: color, transition: 'width .3s' }}/>
      </div>
    </div>
  );
};

const FleetCard = ({ a, selected, onClick }) => {
  const meta = window.STATUS_META[a.status];
  const oilLow = a.consumables.oil < 0.5;
  return (
    <div onClick={onClick} style={{
      background: selected ? v1Pal.panel2 : v1Pal.panel,
      border: '1px solid ' + (selected ? meta.color + '66' : v1Pal.border),
      borderLeft: '3px solid ' + meta.color,
      borderRadius: 4,
      padding: '14px 16px',
      cursor: 'pointer',
      display: 'flex', flexDirection: 'column', gap: 12,
      transition: 'background .12s, border-color .12s',
      position: 'relative',
    }}>
      {a.live && (
        <div style={{
          position: 'absolute', top: 10, right: 12,
          display: 'flex', alignItems: 'center', gap: 5,
          fontSize: 9, fontFamily: v1Pal.mono, fontWeight: 600,
          color: v1Pal.green, letterSpacing: '0.1em',
        }}>
          <StatusDot color={v1Pal.green} pulse/>
          IN SIM
        </div>
      )}

      {/* Header row */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 12 }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 3, minWidth: 0 }}>
          <div style={{ fontFamily: v1Pal.mono, fontSize: 16, fontWeight: 700, color: v1Pal.text, letterSpacing: '0.02em' }}>
            {a.tail}
          </div>
          <div style={{ fontSize: 11, color: v1Pal.dim, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {a.model}
          </div>
        </div>
        {!a.live && <Pill color={meta.color}>{meta.label}</Pill>}
      </div>

      {/* Stats grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 10, paddingTop: 10, borderTop: '1px dashed ' + v1Pal.border }}>
        <Stat label="HOURS" value={a.hours.toFixed(1)} />
        <Stat label="CYCLES" value={a.cycles.toLocaleString()} />
        <Stat label="NEXT MX" value={a.nextInspectionHrs.toFixed(1) + 'h'} alert={a.nextInspectionHrs < 5}/>
      </div>

      {/* Consumables */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 10 }}>
        <WearBar value={a.consumables.oil} label="OIL" low={oilLow}/>
        <WearBar value={a.consumables.tires} label="TIRE"/>
        <WearBar value={a.consumables.brakes} label="BRK"/>
        <WearBar value={a.consumables.battery} label="BAT"/>
      </div>

      {/* Footer chips */}
      <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
        {a.openSquawks > 0 && <Pill color={v1Pal.orange}>{a.openSquawks} OPEN</Pill>}
        {a.deferred > 0 && <Pill color={v1Pal.dim}>{a.deferred} MEL</Pill>}
        {a.openSquawks === 0 && a.deferred === 0 && <Pill color={v1Pal.green}>NO SQUAWKS</Pill>}
      </div>
    </div>
  );
};

const Stat = ({ label, value, alert }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
    <span style={{ fontSize: 9, letterSpacing: '0.1em', color: v1Pal.faint, fontFamily: v1Pal.mono }}>{label}</span>
    <span style={{
      fontFamily: v1Pal.mono, fontSize: 13, fontWeight: 600,
      color: alert ? v1Pal.orange : v1Pal.text,
    }}>{value}</span>
  </div>
);

const SidebarItem = ({ icon, label, active, count, onClick }) => (
  <div onClick={onClick} style={{
    display: 'flex', alignItems: 'center', gap: 12,
    padding: '10px 14px', borderRadius: 4,
    background: active ? 'rgba(212,162,76,.10)' : 'transparent',
    color: active ? v1Pal.accent : v1Pal.dim,
    cursor: 'pointer',
    fontSize: 13, fontWeight: 500,
    borderLeft: '2px solid ' + (active ? v1Pal.accent : 'transparent'),
    transition: 'all .12s',
  }}>
    <span style={{ fontFamily: v1Pal.mono, fontSize: 11, width: 16, textAlign: 'center' }}>{icon}</span>
    <span style={{ flex: 1 }}>{label}</span>
    {count != null && (
      <span style={{
        fontFamily: v1Pal.mono, fontSize: 10,
        padding: '2px 6px', borderRadius: 2,
        background: active ? 'rgba(212,162,76,.15)' : 'rgba(255,255,255,.04)',
      }}>{count}</span>
    )}
  </div>
);

const SimStatus = ({ status }) => {
  const ok = status.simconnect === 'connected';
  return (
    <div style={{
      padding: 12,
      background: 'rgba(0,0,0,.3)',
      borderRadius: 4,
      border: '1px solid ' + v1Pal.border,
      display: 'flex', flexDirection: 'column', gap: 8,
    }}>
      <div style={{ fontSize: 9, letterSpacing: '0.14em', color: v1Pal.faint, fontFamily: v1Pal.mono }}>
        SIM LINK
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, fontFamily: v1Pal.mono, fontSize: 11 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <StatusDot color={ok ? v1Pal.green : v1Pal.red} pulse={ok}/>
          <span style={{ flex: 1, color: v1Pal.dim }}>SimConnect</span>
          <span style={{ color: ok ? v1Pal.green : v1Pal.red }}>{status.simconnect}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <StatusDot color={status.mobiflight === 'connected' ? v1Pal.green : v1Pal.red}/>
          <span style={{ flex: 1, color: v1Pal.dim }}>MobiFlight WASM</span>
          <span style={{ color: status.mobiflight === 'connected' ? v1Pal.green : v1Pal.red }}>{status.mobiflight}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <StatusDot color={v1Pal.dim}/>
          <span style={{ flex: 1, color: v1Pal.dim }}>FSUIPC7</span>
          <span style={{ color: v1Pal.dim }}>{status.fsuipc}</span>
        </div>
      </div>
      {status.aircraftDetected && (
        <div style={{ paddingTop: 8, borderTop: '1px dashed ' + v1Pal.border, fontSize: 10, color: v1Pal.dim }}>
          <div style={{ fontSize: 9, color: v1Pal.faint, letterSpacing: '0.1em', marginBottom: 3, fontFamily: v1Pal.mono }}>DETECTED</div>
          <div style={{ color: v1Pal.text }}>{status.aircraftDetected}</div>
          <div style={{ fontFamily: v1Pal.mono, fontSize: 10, color: v1Pal.faint, marginTop: 2 }}>{status.bindingProfile}</div>
        </div>
      )}
    </div>
  );
};

const LiveAircraftPanel = ({ a }) => {
  const s = a.liveState;
  return (
    <div style={{
      background: 'linear-gradient(180deg, rgba(127,176,105,.06) 0%, rgba(127,176,105,.02) 100%)',
      border: '1px solid rgba(127,176,105,.25)',
      borderRadius: 4,
      padding: 14,
      display: 'flex', flexDirection: 'column', gap: 12,
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <StatusDot color={v1Pal.green} pulse/>
          <span style={{ fontSize: 9, letterSpacing: '0.14em', color: v1Pal.green, fontFamily: v1Pal.mono, fontWeight: 600 }}>LIVE · {s.phase}</span>
        </div>
        <span style={{ fontFamily: v1Pal.mono, fontSize: 11, color: v1Pal.text, fontWeight: 700 }}>{a.tail}</span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, fontFamily: v1Pal.mono, fontSize: 11 }}>
        <LiveStat label="ALT" value={s.altitude.toLocaleString()} unit="ft"/>
        <LiveStat label="IAS" value={s.ias} unit="kt"/>
        <LiveStat label="GS"  value={s.gs} unit="kt"/>
        <LiveStat label="HDG" value={String(s.hdg).padStart(3,'0') + '°'}/>
        <LiveStat label="OAT" value={s.oat + '°C'}/>
        <LiveStat label="FUEL" value={s.fuelKg} unit="kg"/>
      </div>
      <div style={{ paddingTop: 8, borderTop: '1px dashed ' + v1Pal.border, fontFamily: v1Pal.mono, fontSize: 10, color: v1Pal.dim, display: 'flex', justifyContent: 'space-between' }}>
        <span>SESSION {s.session.dur}</span>
        <span>HOBBS +{(s.currentHobbs - s.hobbsStart).toFixed(1)}</span>
        <span>MAX G {s.session.maxG}</span>
      </div>
    </div>
  );
};

const LiveStat = ({ label, value, unit }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
    <span style={{ fontSize: 9, letterSpacing: '0.1em', color: v1Pal.faint }}>{label}</span>
    <span style={{ color: v1Pal.text, fontWeight: 600 }}>
      {value}{unit && <span style={{ color: v1Pal.faint, fontWeight: 400, marginLeft: 3 }}>{unit}</span>}
    </span>
  </div>
);

const Variant1 = () => {
  const [tab, setTab] = React.useState('fleet');
  const [selectedId, setSelectedId] = React.useState('N350KA');
  const [simOk, setSimOk] = React.useState(true);
  const fleet = window.FLEET;
  const liveAc = fleet.find(a => a.live);
  const status = simOk
    ? window.SIM_STATUS_DEFAULT
    : { simconnect: 'offline', mobiflight: 'offline', fsuipc: 'detected' };

  const totalSquawks = window.SQUAWKS.filter(s => s.severity !== 'deferred').length;
  const totalDeferred = window.SQUAWKS.filter(s => s.severity === 'deferred').length;
  const mxDue = fleet.filter(a => a.nextInspectionHrs < 10).length;

  return (
    <window.WindowsFrame title="msfs-failures" subtitle="hangar.exe · v0.1.0-alpha" width={1440} height={900} bg={v1Pal.bg}>
      <style>{`@keyframes v1pulse{0%{box-shadow:0 0 0 0 rgba(127,176,105,.5)}100%{box-shadow:0 0 0 8px rgba(127,176,105,0)}}`}</style>
      <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '240px 1fr', minHeight: 0 }}>
        {/* Sidebar */}
        <aside style={{
          background: '#100e0a',
          borderRight: '1px solid ' + v1Pal.border,
          display: 'flex', flexDirection: 'column',
          padding: '20px 12px',
          gap: 20,
        }}>
          <div style={{ padding: '0 8px' }}>
            <div style={{ fontFamily: v1Pal.mono, fontSize: 14, fontWeight: 700, letterSpacing: '0.04em', color: v1Pal.text }}>
              MSFS-FAILURES
            </div>
            <div style={{ fontSize: 10, color: v1Pal.faint, marginTop: 2, fontFamily: v1Pal.mono }}>
              MAINTENANCE HANGAR
            </div>
          </div>

          <nav style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <div style={{ fontSize: 9, letterSpacing: '0.14em', color: v1Pal.faint, fontFamily: v1Pal.mono, padding: '4px 14px 6px' }}>
              OPERATIONS
            </div>
            <SidebarItem icon="◇" label="Hangar" active={tab === 'fleet'} count={fleet.length} onClick={() => setTab('fleet')}/>
            <SidebarItem icon="!"  label="Squawks" active={tab === 'squawks'} count={totalSquawks} onClick={() => setTab('squawks')}/>
            <SidebarItem icon="↻"  label="Maintenance" active={tab === 'mx'} count={mxDue} onClick={() => setTab('mx')}/>
            <SidebarItem icon="○"  label="Sessions" active={tab === 'sessions'} onClick={() => setTab('sessions')}/>

            <div style={{ fontSize: 9, letterSpacing: '0.14em', color: v1Pal.faint, fontFamily: v1Pal.mono, padding: '20px 14px 6px' }}>
              CONFIG
            </div>
            <SidebarItem icon="≡" label="Airframe Editor" onClick={() => {}}/>
            <SidebarItem icon="⚙" label="Bindings" onClick={() => {}}/>
            <SidebarItem icon="◐" label="Settings" onClick={() => {}}/>
          </nav>

          <div style={{ flex: 1 }}/>

          {/* Sim toggle */}
          <SimStatus status={status}/>
          <button onClick={() => setSimOk(v => !v)} style={{
            background: simOk ? 'rgba(194,84,80,.10)' : 'rgba(127,176,105,.10)',
            border: '1px solid ' + (simOk ? 'rgba(194,84,80,.3)' : 'rgba(127,176,105,.3)'),
            color: simOk ? v1Pal.red : v1Pal.green,
            padding: '8px 12px', borderRadius: 4,
            fontFamily: v1Pal.mono, fontSize: 11, fontWeight: 600,
            letterSpacing: '0.06em',
            cursor: 'pointer',
          }}>
            {simOk ? 'DISCONNECT SIM' : 'CONNECT SIM'}
          </button>
        </aside>

        {/* Main */}
        <main style={{ display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'auto' }}>
          {/* Top bar */}
          <header style={{
            padding: '20px 28px',
            display: 'flex', alignItems: 'baseline', justifyContent: 'space-between',
            borderBottom: '1px solid ' + v1Pal.border,
          }}>
            <div>
              <div style={{ fontSize: 22, fontWeight: 600, color: v1Pal.text, letterSpacing: '-0.01em' }}>
                {tab === 'fleet' && 'Hangar'}
                {tab === 'squawks' && 'Squawks'}
                {tab === 'mx' && 'Maintenance'}
                {tab === 'sessions' && 'Sessions'}
              </div>
              <div style={{ fontSize: 12, color: v1Pal.dim, marginTop: 4 }}>
                {fleet.length} airframes · {totalSquawks} open squawks · {totalDeferred} deferred · {mxDue} due for inspection
              </div>
            </div>
            <div style={{ display: 'flex', gap: 10 }}>
              <button style={btnSecondary}>+ NEW AIRFRAME</button>
              <button style={btnPrimary}>FILE SQUAWK</button>
            </div>
          </header>

          {/* Body */}
          {tab === 'fleet' && (
            <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '1fr 320px', minHeight: 0 }}>
              <div style={{ padding: 28, display: 'flex', flexDirection: 'column', gap: 16, overflow: 'auto' }}>
                {liveAc && <LiveAircraftPanel a={liveAc}/>}
                <div style={{ fontSize: 9, letterSpacing: '0.14em', color: v1Pal.faint, fontFamily: v1Pal.mono, marginTop: 4 }}>
                  FLEET · {fleet.length} AIRFRAMES
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
                  {fleet.map(a => (
                    <FleetCard key={a.id} a={a} selected={a.id === selectedId} onClick={() => setSelectedId(a.id)}/>
                  ))}
                </div>
              </div>

              {/* Detail rail */}
              <DetailRail tail={selectedId} pal={v1Pal}/>
            </div>
          )}

          {tab === 'squawks' && <SquawksTable pal={v1Pal}/>}
          {tab === 'mx'      && <MxView pal={v1Pal}/>}
          {tab === 'sessions'&& <SessionsView pal={v1Pal}/>}
        </main>
      </div>
    </window.WindowsFrame>
  );
};

const btnPrimary = {
  background: '#d4a24c', color: '#16140f', border: 'none',
  padding: '8px 14px', borderRadius: 3,
  fontFamily: '"JetBrains Mono", monospace', fontSize: 11, fontWeight: 700,
  letterSpacing: '0.08em', cursor: 'pointer',
};
const btnSecondary = {
  background: 'transparent', color: '#9a9080',
  border: '1px solid rgba(255,255,255,.10)',
  padding: '8px 14px', borderRadius: 3,
  fontFamily: '"JetBrains Mono", monospace', fontSize: 11, fontWeight: 600,
  letterSpacing: '0.08em', cursor: 'pointer',
};

const DetailRail = ({ tail, pal }) => {
  const a = window.FLEET.find(x => x.id === tail);
  if (!a) return null;
  const meta = window.STATUS_META[a.status];
  const sqs = window.SQUAWKS.filter(s => s.tail === a.tail);
  return (
    <aside style={{
      background: pal.panel,
      borderLeft: '1px solid ' + pal.border,
      padding: 22,
      display: 'flex', flexDirection: 'column', gap: 18,
      overflow: 'auto',
    }}>
      <div>
        <div style={{ fontSize: 9, letterSpacing: '0.14em', color: pal.faint, fontFamily: pal.mono }}>SELECTED AIRFRAME</div>
        <div style={{ fontFamily: pal.mono, fontSize: 22, fontWeight: 700, color: pal.text, marginTop: 4 }}>{a.tail}</div>
        <div style={{ fontSize: 12, color: pal.dim, marginTop: 2 }}>{a.model}</div>
        <div style={{ marginTop: 10 }}><Pill color={meta.color}>{meta.label}</Pill></div>
      </div>

      <Section pal={pal} title="HOURS & CYCLES">
        <KV pal={pal} k="Total Hobbs"   v={a.hours.toFixed(1) + ' h'}/>
        <KV pal={pal} k="Cycles"        v={a.cycles.toLocaleString()}/>
        <KV pal={pal} k="Since last MX" v={a.hobbsSinceMx.toFixed(1) + ' h'}/>
        <KV pal={pal} k="Next inspection" v={a.nextInspectionHrs.toFixed(1) + ' h'} alert={a.nextInspectionHrs < 5}/>
      </Section>

      <Section pal={pal} title="LAST SESSION">
        <KV pal={pal} k="Date"    v={a.lastFlight.date}/>
        <KV pal={pal} k="Duration" v={a.lastFlight.dur}/>
        <KV pal={pal} k="Max G"   v={a.lastFlight.maxG.toFixed(1)} alert={a.lastFlight.maxG > 2}/>
        <KV pal={pal} k="Hard landings" v={a.lastFlight.hardLandings} alert={a.lastFlight.hardLandings > 0}/>
        <KV pal={pal} k="Overtemps"     v={a.lastFlight.overtemps} alert={a.lastFlight.overtemps > 0}/>
      </Section>

      <Section pal={pal} title={`SQUAWKS (${sqs.length})`}>
        {sqs.length === 0 && (
          <div style={{ fontSize: 11, color: pal.dim, fontStyle: 'italic' }}>No open squawks. Airworthy.</div>
        )}
        {sqs.map(s => (
          <div key={s.id} style={{
            padding: '10px 0',
            borderBottom: '1px dashed ' + pal.border,
            display: 'flex', flexDirection: 'column', gap: 4,
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
              <span style={{ fontFamily: pal.mono, fontSize: 11, color: pal.text, fontWeight: 600 }}>{s.component}</span>
              <span style={{ fontFamily: pal.mono, fontSize: 9, color: s.severity === 'grounding' ? pal.red : s.severity === 'deferred' ? pal.dim : pal.orange, fontWeight: 600, letterSpacing: '0.08em' }}>
                {s.severity.toUpperCase()}
              </span>
            </div>
            <div style={{ fontSize: 11, color: pal.dim, lineHeight: 1.5 }}>{s.summary}</div>
          </div>
        ))}
      </Section>

      <Section pal={pal} title="QUICK ACTIONS">
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
          <button style={{...btnSecondary, padding: '8px 6px', fontSize: 10}}>+ SQUAWK</button>
          <button style={{...btnSecondary, padding: '8px 6px', fontSize: 10}}>↳ DEFER</button>
          <button style={{...btnSecondary, padding: '8px 6px', fontSize: 10}}>✓ SERVICE</button>
          <button style={{...btnSecondary, padding: '8px 6px', fontSize: 10, color: pal.red, borderColor: 'rgba(194,84,80,.3)'}}>⚠ GROUND</button>
        </div>
      </Section>
    </aside>
  );
};

const Section = ({ pal, title, children }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
    <div style={{ fontSize: 9, letterSpacing: '0.14em', color: pal.faint, fontFamily: pal.mono }}>{title}</div>
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>{children}</div>
  </div>
);

const KV = ({ pal, k, v, alert }) => (
  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', fontSize: 11, fontFamily: pal.mono }}>
    <span style={{ color: pal.dim }}>{k}</span>
    <span style={{ color: alert ? pal.orange : pal.text, fontWeight: 600 }}>{v}</span>
  </div>
);

const SquawksTable = ({ pal }) => (
  <div style={{ padding: 28, overflow: 'auto' }}>
    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
      <thead>
        <tr style={{ borderBottom: '1px solid ' + pal.border }}>
          {['ID', 'TAIL', 'COMPONENT', 'SUMMARY', 'OPENED', 'STATUS', ''].map(h => (
            <th key={h} style={{
              padding: '10px 14px', textAlign: 'left',
              fontSize: 9, letterSpacing: '0.14em', color: pal.faint, fontFamily: pal.mono, fontWeight: 500,
            }}>{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {window.SQUAWKS.map(s => (
          <tr key={s.id} style={{ borderBottom: '1px solid ' + pal.border }}>
            <td style={{ padding: '14px', fontFamily: pal.mono, color: pal.dim, fontSize: 11 }}>{s.id}</td>
            <td style={{ padding: '14px', fontFamily: pal.mono, color: pal.text, fontWeight: 600 }}>{s.tail}</td>
            <td style={{ padding: '14px', color: pal.text, fontWeight: 500 }}>{s.component}</td>
            <td style={{ padding: '14px', color: pal.dim, maxWidth: 380 }}>{s.summary}</td>
            <td style={{ padding: '14px', fontFamily: pal.mono, color: pal.dim, fontSize: 11 }}>{s.opened}</td>
            <td style={{ padding: '14px' }}>
              <Pill color={s.severity === 'grounding' ? pal.red : s.severity === 'deferred' ? pal.dim : pal.orange}>
                {s.severity}
              </Pill>
            </td>
            <td style={{ padding: '14px', textAlign: 'right' }}>
              <button style={{...btnSecondary, padding: '4px 10px', fontSize: 10}}>OPEN</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);

const MxView = ({ pal }) => (
  <div style={{ padding: 28, display: 'flex', flexDirection: 'column', gap: 18 }}>
    {window.FLEET.map(a => (
      <div key={a.id} style={{
        background: pal.panel, border: '1px solid ' + pal.border, borderRadius: 4,
        padding: 18, display: 'grid', gridTemplateColumns: '160px 1fr 200px', alignItems: 'center', gap: 20,
      }}>
        <div>
          <div style={{ fontFamily: pal.mono, fontSize: 14, fontWeight: 700, color: pal.text }}>{a.tail}</div>
          <div style={{ fontSize: 11, color: pal.dim, marginTop: 2 }}>{a.type}</div>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 12 }}>
          <WearBar value={a.consumables.oil} label="OIL"/>
          <WearBar value={a.consumables.tires} label="TIRES"/>
          <WearBar value={a.consumables.brakes} label="BRAKES"/>
          <WearBar value={a.consumables.battery} label="BATT SOH"/>
          <WearBar value={a.consumables.hyd} label="HYD"/>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 9, color: pal.faint, fontFamily: pal.mono, letterSpacing: '0.1em' }}>NEXT INSPECTION</div>
          <div style={{ fontFamily: pal.mono, fontSize: 16, fontWeight: 700, color: a.nextInspectionHrs < 5 ? pal.orange : pal.text, marginTop: 3 }}>
            {a.nextInspectionHrs.toFixed(1)} h
          </div>
        </div>
      </div>
    ))}
  </div>
);

const SessionsView = ({ pal }) => (
  <div style={{ padding: 28, color: pal.dim, fontSize: 13 }}>
    Sessions log — coming next iteration. (Per-flight Hobbs, max G, overtemps, hard landings,
    component wear deltas.)
  </div>
);

window.Variant1 = Variant1;
