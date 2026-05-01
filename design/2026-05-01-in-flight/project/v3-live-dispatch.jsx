// Variant 3 — Live-first dispatch.
// Glass-cockpit influence: large live state hero up top, fleet ribbon below.
// Greens/ambers/reds against deep black. EFB-lite.

const v3Pal = {
  bg: '#0a0c0f',
  panel: '#11141a',
  panel2: '#161a22',
  border: 'rgba(255,255,255,.05)',
  text: '#dfe3e8',
  dim: '#7d848f',
  faint: '#454b54',
  green: '#5dd39e',
  amber: '#e8b85a',
  orange: '#e8945a',
  red: '#e85a5a',
  cyan: '#5fb8d6',
  mono: '"JetBrains Mono", "SF Mono", Consolas, monospace',
};

const Gauge = ({ label, value, unit, max, min, redline, caution, large }) => {
  const pct = Math.max(0, Math.min(1, (value - (min ?? 0)) / (max - (min ?? 0))));
  const overRed = redline != null && value > redline;
  const overAmber = caution != null && value > caution && !overRed;
  const color = overRed ? v3Pal.red : overAmber ? v3Pal.amber : v3Pal.green;
  return (
    <div style={{
      padding: large ? 14 : 10,
      background: v3Pal.panel,
      border: '1px solid ' + (overRed ? v3Pal.red + '55' : v3Pal.border),
      borderRadius: 3,
      display: 'flex', flexDirection: 'column', gap: 6,
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <span style={{ fontFamily: v3Pal.mono, fontSize: 9, letterSpacing: '0.14em', color: v3Pal.faint }}>{label}</span>
        {unit && <span style={{ fontFamily: v3Pal.mono, fontSize: 9, color: v3Pal.faint }}>{unit}</span>}
      </div>
      <div style={{
        fontFamily: v3Pal.mono,
        fontSize: large ? 30 : 22,
        fontWeight: 700,
        color: color,
        lineHeight: 1,
        letterSpacing: '-0.02em',
      }}>
        {typeof value === 'number' && value % 1 !== 0 ? value.toFixed(1) : value}
      </div>
      {max && (
        <div style={{ height: 2, background: 'rgba(255,255,255,.04)', position: 'relative' }}>
          <div style={{ width: pct * 100 + '%', height: '100%', background: color, transition: 'width .3s' }}/>
          {caution && (
            <div style={{ position: 'absolute', left: ((caution - (min ?? 0)) / (max - (min ?? 0))) * 100 + '%', top: -2, width: 1, height: 6, background: v3Pal.amber }}/>
          )}
          {redline && (
            <div style={{ position: 'absolute', left: ((redline - (min ?? 0)) / (max - (min ?? 0))) * 100 + '%', top: -2, width: 1, height: 6, background: v3Pal.red }}/>
          )}
        </div>
      )}
    </div>
  );
};

const FleetRibbonCard = ({ a, selected, onClick }) => {
  const meta = window.STATUS_META[a.status];
  const sevColor = meta.color;
  return (
    <div onClick={onClick} style={{
      flex: '1 1 0', minWidth: 180,
      padding: 12,
      background: a.live ? 'linear-gradient(180deg, rgba(93,211,158,.12), rgba(93,211,158,.02))' : (selected ? v3Pal.panel2 : v3Pal.panel),
      border: '1px solid ' + (a.live ? 'rgba(93,211,158,.3)' : (selected ? sevColor + '55' : v3Pal.border)),
      borderRadius: 3,
      cursor: 'pointer',
      display: 'flex', flexDirection: 'column', gap: 8,
      position: 'relative',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <span style={{ fontFamily: v3Pal.mono, fontSize: 13, fontWeight: 700, color: v3Pal.text, letterSpacing: '0.02em' }}>{a.tail}</span>
        {a.live ? (
          <span style={{ fontFamily: v3Pal.mono, fontSize: 9, color: v3Pal.green, fontWeight: 700, letterSpacing: '0.1em' }}>● LIVE</span>
        ) : (
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: sevColor }}/>
        )}
      </div>
      <div style={{ fontSize: 10, color: v3Pal.dim }}>{a.type}</div>
      <div style={{ height: 1, background: v3Pal.border }}/>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontFamily: v3Pal.mono, fontSize: 10, color: v3Pal.dim }}>
        <span>{a.hours.toFixed(0)} h</span>
        <span style={{ color: a.openSquawks ? v3Pal.orange : v3Pal.faint }}>{a.openSquawks} sq</span>
        <span style={{ color: a.nextInspectionHrs < 5 ? v3Pal.amber : v3Pal.faint }}>
          {a.nextInspectionHrs.toFixed(0)}h MX
        </span>
      </div>
    </div>
  );
};

const Variant3 = () => {
  const [selectedTail, setSelectedTail] = React.useState('N350KA');
  const [tab, setTab] = React.useState('live');
  const [simOk, setSimOk] = React.useState(true);
  const liveAc = window.FLEET.find(a => a.live);
  const sel = window.FLEET.find(a => a.tail === selectedTail);
  const ls = liveAc?.liveState;

  return (
    <window.WindowsFrame title="msfs-failures" subtitle="LIVE · BE350 binding profile loaded" width={1440} height={900} accent={v3Pal.green} bg={v3Pal.bg}>
      {/* Top bar */}
      <header style={{
        height: 48, flex: '0 0 48px',
        background: v3Pal.panel,
        borderBottom: '1px solid ' + v3Pal.border,
        display: 'flex', alignItems: 'center',
        paddingLeft: 20,
        gap: 24,
      }}>
        <div style={{ fontFamily: v3Pal.mono, fontSize: 12, fontWeight: 700, color: v3Pal.text, letterSpacing: '0.06em' }}>
          MSFS-FAILURES
        </div>
        <div style={{ width: 1, height: 20, background: v3Pal.border }}/>
        <nav style={{ display: 'flex', gap: 4 }}>
          {[
            { k: 'live', l: 'Live' },
            { k: 'fleet', l: 'Fleet' },
            { k: 'squawks', l: 'Squawks', n: window.SQUAWKS.filter(s => s.severity !== 'deferred').length },
            { k: 'mx', l: 'Maintenance' },
          ].map(t => (
            <button key={t.k} onClick={() => setTab(t.k)} style={{
              background: tab === t.k ? 'rgba(93,211,158,.10)' : 'transparent',
              color: tab === t.k ? v3Pal.green : v3Pal.dim,
              border: 'none', padding: '6px 12px', borderRadius: 2,
              fontFamily: v3Pal.mono, fontSize: 10, fontWeight: 600, letterSpacing: '0.1em',
              cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6,
            }}>
              {t.l.toUpperCase()}
              {t.n != null && t.n > 0 && (
                <span style={{ fontSize: 9, padding: '1px 5px', borderRadius: 1, background: v3Pal.orange + '22', color: v3Pal.orange }}>{t.n}</span>
              )}
            </button>
          ))}
        </nav>
        <div style={{ flex: 1 }}/>
        <div style={{ fontFamily: v3Pal.mono, fontSize: 10, color: v3Pal.faint, letterSpacing: '0.1em' }}>
          {window.SIM_STATUS_DEFAULT.uptime}
        </div>
        <button onClick={() => setSimOk(v => !v)} style={{
          background: simOk ? 'rgba(93,211,158,.08)' : 'rgba(232,90,90,.08)',
          color: simOk ? v3Pal.green : v3Pal.red,
          border: '1px solid ' + (simOk ? 'rgba(93,211,158,.3)' : 'rgba(232,90,90,.3)'),
          padding: '5px 12px', borderRadius: 2, marginRight: 16,
          fontFamily: v3Pal.mono, fontSize: 10, fontWeight: 600, letterSpacing: '0.1em',
          cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6,
        }}>
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: simOk ? v3Pal.green : v3Pal.red, animation: simOk ? 'v3pulse 2s infinite' : 'none' }}/>
          {simOk ? 'SIM CONNECTED' : 'SIM OFFLINE'}
        </button>
      </header>

      {/* Hero: live aircraft */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'auto' }}>
        {liveAc && simOk && (
          <section style={{
            padding: '20px 24px',
            background: 'linear-gradient(180deg, rgba(93,211,158,.04) 0%, transparent 60%)',
            borderBottom: '1px solid ' + v3Pal.border,
            display: 'flex', flexDirection: 'column', gap: 16,
          }}>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 16 }}>
              <span style={{ width: 9, height: 9, borderRadius: '50%', background: v3Pal.green, animation: 'v3pulse 2s infinite' }}/>
              <span style={{ fontFamily: v3Pal.mono, fontSize: 11, color: v3Pal.green, fontWeight: 700, letterSpacing: '0.16em' }}>LIVE · {ls.phase}</span>
              <span style={{ width: 1, height: 16, background: v3Pal.border }}/>
              <span style={{ fontFamily: v3Pal.mono, fontSize: 22, fontWeight: 700, color: v3Pal.text, letterSpacing: '0.02em' }}>{liveAc.tail}</span>
              <span style={{ fontSize: 13, color: v3Pal.dim }}>{liveAc.model}</span>
              <span style={{ flex: 1 }}/>
              <span style={{ fontFamily: v3Pal.mono, fontSize: 10, color: v3Pal.faint, letterSpacing: '0.08em' }}>
                BINDING · {window.SIM_STATUS_DEFAULT.bindingProfile}
              </span>
            </div>

            {/* Primary flight + engine row */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr) 1.4fr', gap: 10 }}>
              <Gauge label="ALT"  value={ls.altitude.toLocaleString()} unit="ft" large/>
              <Gauge label="IAS"  value={ls.ias} unit="kt" large/>
              <Gauge label="GS"   value={ls.gs} unit="kt" large/>
              <Gauge label="HDG"  value={String(ls.hdg).padStart(3,'0')} unit="°M" large/>
              <Gauge label="OAT"  value={ls.oat} unit="°C" large/>
              <Gauge label="FUEL" value={ls.fuelKg} unit="kg" large/>
              {/* Session */}
              <div style={{
                padding: 14, background: v3Pal.panel, border: '1px solid ' + v3Pal.border, borderRadius: 3,
                display: 'flex', flexDirection: 'column', gap: 6, fontFamily: v3Pal.mono,
              }}>
                <div style={{ fontSize: 9, letterSpacing: '0.14em', color: v3Pal.faint }}>SESSION</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6, fontSize: 11 }}>
                  <div><span style={{ color: v3Pal.faint }}>DUR </span><span style={{ color: v3Pal.text, fontWeight: 600 }}>{ls.session.dur}</span></div>
                  <div><span style={{ color: v3Pal.faint }}>HOBBS </span><span style={{ color: v3Pal.text, fontWeight: 600 }}>+{(ls.currentHobbs - ls.hobbsStart).toFixed(1)}</span></div>
                  <div><span style={{ color: v3Pal.faint }}>MAX G </span><span style={{ color: v3Pal.text, fontWeight: 600 }}>{ls.session.maxG}</span></div>
                  <div><span style={{ color: v3Pal.faint }}>OT </span><span style={{ color: ls.session.overtemps ? v3Pal.amber : v3Pal.text, fontWeight: 600 }}>{ls.session.overtemps}</span></div>
                </div>
              </div>
            </div>

            {/* Engines */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
              {[0,1].map(i => (
                <div key={i} style={{
                  padding: 14, background: v3Pal.panel, border: '1px solid ' + v3Pal.border, borderRadius: 3,
                  display: 'flex', flexDirection: 'column', gap: 10,
                }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                    <span style={{ fontFamily: v3Pal.mono, fontSize: 11, fontWeight: 700, letterSpacing: '0.12em', color: v3Pal.text }}>
                      ENGINE {i+1} · PT6A-67
                    </span>
                    <span style={{ fontFamily: v3Pal.mono, fontSize: 9, color: v3Pal.green, letterSpacing: '0.1em' }}>● NOMINAL</span>
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 8 }}>
                    <Gauge label="N1"     value={ls.n1[i]}     unit="%"   max={104} caution={101} redline={103}/>
                    <Gauge label="ITT"    value={ls.itt[i]}    unit="°C"  max={840} caution={780} redline={825}/>
                    <Gauge label="TQ"     value={ls.torque[i]} unit="%"   max={100} caution={92}  redline={98}/>
                    <Gauge label="OIL T"  value={ls.oilTemp[i]}unit="°C"  max={110} caution={99}/>
                    <Gauge label="OIL P"  value={ls.oilPress[i]}unit="psi" min={40} max={130}/>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        {!simOk && (
          <div style={{
            padding: '40px 24px',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            borderBottom: '1px solid ' + v3Pal.border,
            background: 'rgba(232,90,90,.03)',
          }}>
            <div style={{ textAlign: 'center', display: 'flex', flexDirection: 'column', gap: 6 }}>
              <div style={{ fontFamily: v3Pal.mono, fontSize: 10, color: v3Pal.red, letterSpacing: '0.2em', fontWeight: 700 }}>
                ◇ NO SIM CONNECTION
              </div>
              <div style={{ fontSize: 13, color: v3Pal.dim }}>SimConnect offline. Live state unavailable.</div>
              <div style={{ fontSize: 11, color: v3Pal.faint, fontFamily: v3Pal.mono, marginTop: 4 }}>
                Reconnect to resume the 4 Hz tick loop.
              </div>
            </div>
          </div>
        )}

        {/* Fleet ribbon */}
        <section style={{ padding: '18px 24px', display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: 14 }}>
            <span style={{ fontFamily: v3Pal.mono, fontSize: 9, letterSpacing: '0.16em', color: v3Pal.faint }}>FLEET</span>
            <span style={{ fontSize: 11, color: v3Pal.dim }}>{window.FLEET.length} airframes · click to focus</span>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            {window.FLEET.map(a => (
              <FleetRibbonCard key={a.id} a={a} selected={a.tail === selectedTail} onClick={() => setSelectedTail(a.tail)}/>
            ))}
          </div>
        </section>

        {/* Selected airframe details + recent squawks */}
        <section style={{ padding: '4px 24px 24px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
          {sel && <SelectedPanel a={sel}/>}
          <RecentSquawksPanel/>
        </section>
      </div>
      <style>{`@keyframes v3pulse{0%,100%{opacity:1}50%{opacity:.35}}`}</style>
    </window.WindowsFrame>
  );
};

const SelectedPanel = ({ a }) => {
  const meta = window.STATUS_META[a.status];
  return (
    <div style={{
      padding: 16, background: v3Pal.panel, border: '1px solid ' + v3Pal.border, borderRadius: 3,
      display: 'flex', flexDirection: 'column', gap: 12,
    }}>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 12 }}>
        <span style={{ fontFamily: v3Pal.mono, fontSize: 9, letterSpacing: '0.14em', color: v3Pal.faint }}>SELECTED</span>
        <span style={{ fontFamily: v3Pal.mono, fontSize: 18, fontWeight: 700, color: v3Pal.text }}>{a.tail}</span>
        <span style={{ fontSize: 11, color: v3Pal.dim, flex: 1 }}>{a.model}</span>
        <span style={{
          fontFamily: v3Pal.mono, fontSize: 9, fontWeight: 700, letterSpacing: '0.1em',
          color: meta.color, padding: '2px 7px', border: '1px solid ' + meta.color + '55', borderRadius: 2,
        }}>{meta.label}</span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10 }}>
        <KStat label="HOBBS"   v={a.hours.toFixed(1)} u="h"/>
        <KStat label="CYCLES"  v={a.cycles.toLocaleString()}/>
        <KStat label="SINCE MX" v={a.hobbsSinceMx.toFixed(1)} u="h"/>
        <KStat label="NEXT MX"  v={a.nextInspectionHrs.toFixed(1)} u="h" alert={a.nextInspectionHrs < 5}/>
      </div>
      <div style={{ paddingTop: 8, borderTop: '1px dashed ' + v3Pal.border }}>
        <div style={{ fontSize: 9, color: v3Pal.faint, letterSpacing: '0.14em', fontFamily: v3Pal.mono, marginBottom: 8 }}>CONSUMABLES</div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 10 }}>
          {[
            ['oil', 'OIL'], ['tires', 'TIRE'], ['brakes', 'BRK'], ['battery', 'BAT'], ['hyd', 'HYD']
          ].map(([k, l]) => {
            const val = a.consumables[k];
            const c = val < 0.25 ? v3Pal.red : val < 0.5 ? v3Pal.orange : val < 0.75 ? v3Pal.amber : v3Pal.green;
            return (
              <div key={k} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontFamily: v3Pal.mono, fontSize: 9 }}>
                  <span style={{ color: v3Pal.faint, letterSpacing: '0.1em' }}>{l}</span>
                  <span style={{ color: c, fontWeight: 600 }}>{Math.round(val * 100)}%</span>
                </div>
                <div style={{ height: 3, background: 'rgba(255,255,255,.05)' }}>
                  <div style={{ width: val * 100 + '%', height: '100%', background: c }}/>
                </div>
              </div>
            );
          })}
        </div>
      </div>
      <div style={{ display: 'flex', gap: 6 }}>
        <button style={btn3}>+ SQUAWK</button>
        <button style={btn3}>↳ DEFER</button>
        <button style={btn3}>✓ SERVICE</button>
        <button style={{...btn3, color: v3Pal.red, borderColor: 'rgba(232,90,90,.3)'}}>⚠ GROUND</button>
      </div>
    </div>
  );
};

const btn3 = {
  background: 'transparent', color: v3Pal.dim,
  border: '1px solid rgba(255,255,255,.10)',
  padding: '6px 10px', borderRadius: 2,
  fontFamily: v3Pal.mono, fontSize: 10, fontWeight: 600, letterSpacing: '0.1em',
  cursor: 'pointer',
};

const KStat = ({ label, v, u, alert }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
    <span style={{ fontFamily: v3Pal.mono, fontSize: 9, letterSpacing: '0.14em', color: v3Pal.faint }}>{label}</span>
    <span style={{ fontFamily: v3Pal.mono, fontSize: 16, fontWeight: 700, color: alert ? v3Pal.amber : v3Pal.text }}>
      {v}{u && <span style={{ color: v3Pal.faint, fontSize: 11, fontWeight: 400, marginLeft: 3 }}>{u}</span>}
    </span>
  </div>
);

const RecentSquawksPanel = () => {
  const items = [...window.SQUAWKS].sort((a, b) => {
    const o = { grounding: 0, open: 1, deferred: 2 };
    return o[a.severity] - o[b.severity];
  }).slice(0, 5);
  return (
    <div style={{
      padding: 16, background: v3Pal.panel, border: '1px solid ' + v3Pal.border, borderRadius: 3,
      display: 'flex', flexDirection: 'column', gap: 10,
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <span style={{ fontFamily: v3Pal.mono, fontSize: 9, letterSpacing: '0.14em', color: v3Pal.faint }}>RECENT SQUAWKS</span>
        <span style={{ fontFamily: v3Pal.mono, fontSize: 10, color: v3Pal.dim }}>{window.SQUAWKS.length} TOTAL</span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {items.map(s => {
          const c = s.severity === 'grounding' ? v3Pal.red : s.severity === 'open' ? v3Pal.orange : v3Pal.dim;
          return (
            <div key={s.id} style={{ display: 'flex', alignItems: 'center', gap: 10, paddingBottom: 8, borderBottom: '1px dashed ' + v3Pal.border }}>
              <span style={{ width: 6, height: 6, borderRadius: '50%', background: c, flex: '0 0 6px' }}/>
              <span style={{ fontFamily: v3Pal.mono, fontSize: 11, color: v3Pal.text, fontWeight: 700, width: 70 }}>{s.tail}</span>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 12, color: v3Pal.text, fontWeight: 500 }}>{s.component}</div>
                <div style={{ fontSize: 11, color: v3Pal.dim, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{s.summary}</div>
              </div>
              <span style={{ fontFamily: v3Pal.mono, fontSize: 9, color: c, letterSpacing: '0.1em', fontWeight: 700 }}>
                {s.severity.toUpperCase()}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
};

window.Variant3 = Variant3;
