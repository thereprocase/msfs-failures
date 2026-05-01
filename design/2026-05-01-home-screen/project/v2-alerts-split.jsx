// Variant 2 — Alerts-first split-pane.
// Left: prioritized squawk feed dominates; Right: compact fleet column.
// Cooler, more "ops dispatch" feel — slightly bluer warm-gray.

const v2Pal = {
  bg: '#13151a',
  panel: '#191c22',
  panel2: '#20232b',
  border: 'rgba(255,255,255,.06)',
  text: '#e4e6eb',
  dim: '#8b909c',
  faint: '#555a66',
  green: '#7fb069',
  amber: '#d4a24c',
  orange: '#d97757',
  red: '#c25450',
  blue: '#6b9bd1',
  mono: '"JetBrains Mono", "SF Mono", Consolas, monospace',
};

const Sev = ({ s }) => {
  const m = {
    grounding: { c: v2Pal.red, t: 'GROUNDING' },
    open:      { c: v2Pal.orange, t: 'OPEN' },
    deferred:  { c: v2Pal.dim, t: 'MEL DEFERRED' },
  }[s];
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5,
      padding: '2px 7px', borderRadius: 2,
      fontFamily: v2Pal.mono, fontSize: 9, fontWeight: 600,
      letterSpacing: '0.08em',
      color: m.c,
      border: '1px solid ' + m.c + '55',
      background: m.c + '14',
    }}>{m.t}</span>
  );
};

const SquawkRow = ({ s, expanded, onClick }) => {
  const a = window.FLEET.find(x => x.tail === s.tail);
  const sevColor = s.severity === 'grounding' ? v2Pal.red : s.severity === 'open' ? v2Pal.orange : v2Pal.dim;
  return (
    <div onClick={onClick} style={{
      padding: '14px 18px',
      borderLeft: '3px solid ' + sevColor,
      background: expanded ? v2Pal.panel2 : 'transparent',
      borderBottom: '1px solid ' + v2Pal.border,
      cursor: 'pointer',
      display: 'flex', flexDirection: 'column', gap: 8,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <span style={{ fontFamily: v2Pal.mono, fontSize: 10, color: v2Pal.faint }}>{s.id}</span>
        <span style={{ fontFamily: v2Pal.mono, fontSize: 12, fontWeight: 700, color: v2Pal.text }}>{s.tail}</span>
        <span style={{ fontSize: 11, color: v2Pal.dim }}>· {a?.type}</span>
        <div style={{ flex: 1 }}/>
        <Sev s={s.severity}/>
      </div>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 12 }}>
        <span style={{ fontSize: 13, fontWeight: 600, color: v2Pal.text, minWidth: 200 }}>{s.component}</span>
        <span style={{ fontSize: 12, color: v2Pal.dim, flex: 1 }}>{s.summary}</span>
      </div>
      <div style={{ display: 'flex', gap: 14, fontFamily: v2Pal.mono, fontSize: 10, color: v2Pal.faint }}>
        <span>OPENED {s.opened}</span>
        <span>@ {s.hoursAtOpen.toFixed(1)} h</span>
        {s.deferredUntil && <span>UNTIL {s.deferredUntil}</span>}
        {!s.melDeferrable && <span style={{ color: v2Pal.red }}>NOT MEL-DEFERRABLE</span>}
      </div>
      {expanded && (
        <div style={{ paddingTop: 8, borderTop: '1px dashed ' + v2Pal.border, display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div style={{ fontSize: 11, color: v2Pal.dim, lineHeight: 1.6 }}>{s.notes}</div>
          <div style={{ display: 'flex', gap: 6 }}>
            <button style={btn2}>RESOLVE</button>
            {s.melDeferrable && s.severity !== 'deferred' && <button style={btn2}>DEFER (MEL)</button>}
            <button style={btn2}>ASSIGN MX</button>
            <button style={{...btn2, color: v2Pal.red, borderColor: 'rgba(194,84,80,.3)'}}>GROUND AIRFRAME</button>
          </div>
        </div>
      )}
    </div>
  );
};

const btn2 = {
  background: 'transparent', color: v2Pal.dim,
  border: '1px solid rgba(255,255,255,.12)',
  padding: '6px 10px', borderRadius: 2,
  fontFamily: v2Pal.mono, fontSize: 10, fontWeight: 600,
  letterSpacing: '0.08em', cursor: 'pointer',
};

const FleetMini = ({ a, selected, onClick }) => {
  const meta = window.STATUS_META[a.status];
  return (
    <div onClick={onClick} style={{
      padding: '12px 14px',
      borderLeft: '3px solid ' + meta.color,
      background: selected ? v2Pal.panel2 : 'transparent',
      borderBottom: '1px solid ' + v2Pal.border,
      cursor: 'pointer',
      display: 'flex', flexDirection: 'column', gap: 6,
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <span style={{ fontFamily: v2Pal.mono, fontSize: 13, fontWeight: 700, color: v2Pal.text }}>{a.tail}</span>
        {a.live ? (
          <span style={{ fontFamily: v2Pal.mono, fontSize: 9, color: v2Pal.green, fontWeight: 600, letterSpacing: '0.1em' }}>● IN SIM</span>
        ) : (
          <span style={{ fontFamily: v2Pal.mono, fontSize: 9, color: meta.color, fontWeight: 600, letterSpacing: '0.08em' }}>{meta.label}</span>
        )}
      </div>
      <div style={{ fontSize: 11, color: v2Pal.dim }}>{a.type} · {a.nickname}</div>
      <div style={{ display: 'flex', gap: 12, fontFamily: v2Pal.mono, fontSize: 10, color: v2Pal.faint, marginTop: 2 }}>
        <span>{a.hours.toFixed(1)}h</span>
        <span>{a.cycles.toLocaleString()}c</span>
        {a.openSquawks > 0 && <span style={{ color: v2Pal.orange }}>! {a.openSquawks}</span>}
        {a.nextInspectionHrs < 5 && <span style={{ color: v2Pal.amber }}>MX {a.nextInspectionHrs.toFixed(1)}h</span>}
      </div>
    </div>
  );
};

const Variant2 = () => {
  const [expandedId, setExpandedId] = React.useState('SQ-1042');
  const [tab, setTab] = React.useState('squawks');
  const [selectedTail, setSelectedTail] = React.useState('N812RP');
  const [simOk, setSimOk] = React.useState(true);

  const sortedSquawks = [...window.SQUAWKS].sort((a, b) => {
    const order = { grounding: 0, open: 1, deferred: 2 };
    return order[a.severity] - order[b.severity];
  });
  const counts = {
    grounding: window.SQUAWKS.filter(s => s.severity === 'grounding').length,
    open: window.SQUAWKS.filter(s => s.severity === 'open').length,
    deferred: window.SQUAWKS.filter(s => s.severity === 'deferred').length,
  };
  const liveAc = window.FLEET.find(a => a.live);

  return (
    <window.WindowsFrame title="msfs-failures" subtitle="OPS · dispatch view" width={1440} height={900} accent={v2Pal.orange} bg={v2Pal.bg}>
      {/* Top bar with tabs */}
      <header style={{
        height: 54, flex: '0 0 54px',
        background: v2Pal.panel,
        borderBottom: '1px solid ' + v2Pal.border,
        display: 'flex', alignItems: 'stretch',
        paddingLeft: 24,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 28, paddingRight: 32, borderRight: '1px solid ' + v2Pal.border }}>
          <div>
            <div style={{ fontFamily: v2Pal.mono, fontSize: 13, fontWeight: 700, color: v2Pal.text, letterSpacing: '0.04em' }}>MSFS-FAILURES</div>
            <div style={{ fontSize: 9, color: v2Pal.faint, fontFamily: v2Pal.mono, letterSpacing: '0.1em' }}>OPS · DISPATCH</div>
          </div>
        </div>
        <nav style={{ display: 'flex', alignItems: 'stretch' }}>
          {[
            { k: 'squawks', l: 'Squawks', n: counts.open + counts.grounding },
            { k: 'fleet',   l: 'Fleet', n: window.FLEET.length },
            { k: 'mx',      l: 'Maintenance', n: window.FLEET.filter(a => a.nextInspectionHrs < 10).length },
            { k: 'editor',  l: 'Airframe Editor' },
            { k: 'bind',    l: 'Bindings' },
          ].map(t => (
            <button key={t.k} onClick={() => setTab(t.k)} style={{
              background: 'transparent', border: 'none',
              padding: '0 18px',
              color: tab === t.k ? v2Pal.text : v2Pal.dim,
              borderBottom: '2px solid ' + (tab === t.k ? v2Pal.orange : 'transparent'),
              fontSize: 13, fontWeight: 500,
              cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 8,
            }}>
              {t.l}
              {t.n != null && t.n > 0 && (
                <span style={{
                  fontFamily: v2Pal.mono, fontSize: 10,
                  padding: '2px 6px', borderRadius: 2,
                  background: tab === t.k ? v2Pal.orange + '22' : 'rgba(255,255,255,.05)',
                  color: tab === t.k ? v2Pal.orange : v2Pal.dim,
                }}>{t.n}</span>
              )}
            </button>
          ))}
        </nav>
        <div style={{ flex: 1 }}/>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, paddingRight: 20 }}>
          <button onClick={() => setSimOk(v => !v)} style={{
            background: 'transparent',
            border: '1px solid ' + (simOk ? 'rgba(127,176,105,.3)' : 'rgba(194,84,80,.3)'),
            color: simOk ? v2Pal.green : v2Pal.red,
            padding: '6px 12px', borderRadius: 2,
            fontFamily: v2Pal.mono, fontSize: 10, fontWeight: 600,
            letterSpacing: '0.1em', cursor: 'pointer',
            display: 'flex', alignItems: 'center', gap: 6,
          }}>
            <span style={{
              width: 6, height: 6, borderRadius: '50%',
              background: simOk ? v2Pal.green : v2Pal.red,
            }}/>
            {simOk ? 'SIM LINK · 4 Hz' : 'SIM OFFLINE'}
          </button>
        </div>
      </header>

      {/* Body */}
      <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '1fr 360px', minHeight: 0 }}>
        {/* Left: alert feed */}
        <main style={{ display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'auto' }}>
          {/* Alert summary banner */}
          <div style={{
            padding: '20px 28px',
            borderBottom: '1px solid ' + v2Pal.border,
            display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14,
          }}>
            <Banner pal={v2Pal} count={counts.grounding} label="GROUNDING" sub="airframes unsafe for flight" color={v2Pal.red}/>
            <Banner pal={v2Pal} count={counts.open}      label="OPEN" sub="require attention" color={v2Pal.orange}/>
            <Banner pal={v2Pal} count={counts.deferred}  label="DEFERRED" sub="MEL items in monitoring" color={v2Pal.dim}/>
            <Banner pal={v2Pal} count={window.FLEET.filter(a => a.nextInspectionHrs < 10).length} label="MX DUE" sub="< 10 h to inspection" color={v2Pal.amber}/>
          </div>

          <div style={{ padding: '14px 28px 6px', display: 'flex', alignItems: 'baseline', gap: 12 }}>
            <span style={{ fontSize: 9, letterSpacing: '0.14em', color: v2Pal.faint, fontFamily: v2Pal.mono }}>SQUAWK FEED</span>
            <span style={{ fontSize: 11, color: v2Pal.dim }}>sorted by severity · click to expand</span>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column' }}>
            {sortedSquawks.map(s => (
              <SquawkRow key={s.id} s={s} expanded={expandedId === s.id} onClick={() => setExpandedId(expandedId === s.id ? null : s.id)}/>
            ))}
          </div>
        </main>

        {/* Right: compact fleet */}
        <aside style={{
          background: v2Pal.panel,
          borderLeft: '1px solid ' + v2Pal.border,
          display: 'flex', flexDirection: 'column', minHeight: 0,
        }}>
          {liveAc && (
            <div style={{
              padding: 18,
              background: 'linear-gradient(180deg, rgba(127,176,105,.08), transparent)',
              borderBottom: '1px solid ' + v2Pal.border,
              display: 'flex', flexDirection: 'column', gap: 10,
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ width: 7, height: 7, borderRadius: '50%', background: v2Pal.green, animation: 'v2pulse 2s infinite' }}/>
                <span style={{ fontFamily: v2Pal.mono, fontSize: 9, color: v2Pal.green, fontWeight: 700, letterSpacing: '0.14em' }}>LIVE · {liveAc.liveState.phase}</span>
                <span style={{ flex: 1 }}/>
                <span style={{ fontFamily: v2Pal.mono, fontSize: 12, color: v2Pal.text, fontWeight: 700 }}>{liveAc.tail}</span>
              </div>
              <div style={{ fontSize: 11, color: v2Pal.dim }}>{liveAc.model}</div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8, fontFamily: v2Pal.mono, fontSize: 11 }}>
                <Mini label="ALT" v={liveAc.liveState.altitude.toLocaleString()}/>
                <Mini label="IAS" v={liveAc.liveState.ias}/>
                <Mini label="HDG" v={String(liveAc.liveState.hdg).padStart(3,'0')}/>
                <Mini label="OAT" v={liveAc.liveState.oat + 'C'}/>
                <Mini label="N1"  v={liveAc.liveState.n1[0].toFixed(0)}/>
                <Mini label="ITT" v={liveAc.liveState.itt[0]}/>
              </div>
            </div>
          )}
          <div style={{ padding: '14px 18px 6px', fontSize: 9, letterSpacing: '0.14em', color: v2Pal.faint, fontFamily: v2Pal.mono }}>
            FLEET · {window.FLEET.length}
          </div>
          <div style={{ flex: 1, overflow: 'auto' }}>
            {window.FLEET.map(a => (
              <FleetMini key={a.id} a={a} selected={a.tail === selectedTail} onClick={() => setSelectedTail(a.tail)}/>
            ))}
          </div>
        </aside>
      </div>
      <style>{`@keyframes v2pulse{0%,100%{opacity:1}50%{opacity:.4}}`}</style>
    </window.WindowsFrame>
  );
};

const Banner = ({ pal, count, label, sub, color }) => (
  <div style={{
    padding: 14,
    background: pal.panel,
    border: '1px solid ' + pal.border,
    borderRadius: 3,
    borderLeft: '3px solid ' + color,
    display: 'flex', flexDirection: 'column', gap: 4,
  }}>
    <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
      <span style={{ fontFamily: pal.mono, fontSize: 24, fontWeight: 700, color: count > 0 ? color : pal.dim }}>
        {String(count).padStart(2, '0')}
      </span>
      <span style={{ fontFamily: pal.mono, fontSize: 9, fontWeight: 600, letterSpacing: '0.14em', color: color }}>
        {label}
      </span>
    </div>
    <div style={{ fontSize: 11, color: pal.dim }}>{sub}</div>
  </div>
);

const Mini = ({ label, v }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
    <span style={{ fontSize: 9, color: v2Pal.faint, letterSpacing: '0.1em' }}>{label}</span>
    <span style={{ color: v2Pal.text, fontWeight: 600 }}>{v}</span>
  </div>
);

window.Variant2 = Variant2;
