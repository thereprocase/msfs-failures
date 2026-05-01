// Variant 4 — Top-nav data terminal.
// Densest. Mono-heavy. Terminal-style header strip + tabular fleet manifest.
// Closer to "maintenance shop logbook software".

const v4Pal = {
  bg: '#0e0d0a',
  panel: '#15130f',
  panel2: '#1c1a14',
  border: 'rgba(255,255,255,.06)',
  rule: 'rgba(255,255,255,.04)',
  text: '#ddd6c4',
  dim: '#8a8270',
  faint: '#54503f',
  green: '#a8c97a',
  amber: '#d4a24c',
  orange: '#d97757',
  red: '#c25450',
  mono: '"JetBrains Mono", "SF Mono", Consolas, monospace',
};

const TermHeader = ({ simOk, onToggle }) => {
  const now = '2026-04-30 14:22:08Z';
  return (
    <div style={{
      background: '#0a0907',
      borderBottom: '1px solid ' + v4Pal.border,
      padding: '8px 18px',
      display: 'flex', alignItems: 'center', gap: 18,
      fontFamily: v4Pal.mono, fontSize: 11,
      color: v4Pal.dim,
      letterSpacing: '0.04em',
    }}>
      <span style={{ color: v4Pal.amber, fontWeight: 700 }}>msfs-failures</span>
      <span style={{ color: v4Pal.faint }}>v0.1.0-alpha</span>
      <span style={{ color: v4Pal.faint }}>·</span>
      <span style={{ color: v4Pal.dim }}>{now}</span>
      <span style={{ color: v4Pal.faint }}>·</span>
      <span>UTC · UPTIME {window.SIM_STATUS_DEFAULT.uptime}</span>
      <span style={{ flex: 1 }}/>
      <span style={{ color: v4Pal.faint }}>FLEET.DB</span>
      <span style={{ color: v4Pal.dim }}>%LOCALAPPDATA%\\MsfsFailures\\fleet.db</span>
      <span style={{ color: v4Pal.faint }}>·</span>
      <span onClick={onToggle} style={{
        cursor: 'pointer',
        color: simOk ? v4Pal.green : v4Pal.red,
        fontWeight: 700,
        display: 'inline-flex', alignItems: 'center', gap: 6,
      }}>
        <span style={{ width: 6, height: 6, borderRadius: '50%', background: simOk ? v4Pal.green : v4Pal.red, animation: simOk ? 'v4pulse 2s infinite' : 'none' }}/>
        {simOk ? 'SIMCONNECT●WASM●' : 'SIM●OFFLINE'}
      </span>
    </div>
  );
};

const v4Cell = (extra = {}) => ({
  padding: '10px 14px',
  fontFamily: v4Pal.mono,
  fontSize: 11,
  color: v4Pal.text,
  borderBottom: '1px solid ' + v4Pal.rule,
  whiteSpace: 'nowrap',
  ...extra,
});

const Variant4 = () => {
  const [tab, setTab] = React.useState('fleet');
  const [selectedTail, setSelectedTail] = React.useState('N350KA');
  const [simOk, setSimOk] = React.useState(true);
  const liveAc = window.FLEET.find(a => a.live);
  const sel = window.FLEET.find(a => a.tail === selectedTail);
  const ls = liveAc?.liveState;

  return (
    <window.WindowsFrame title="msfs-failures" subtitle="terminal" width={1440} height={900} bg={v4Pal.bg}>
      <TermHeader simOk={simOk} onToggle={() => setSimOk(v => !v)}/>

      {/* Tabs */}
      <div style={{
        display: 'flex', borderBottom: '1px solid ' + v4Pal.border,
        background: '#100e0a',
        paddingLeft: 12,
      }}>
        {[
          { k: 'fleet',    l: 'FLEET',    n: window.FLEET.length },
          { k: 'squawks',  l: 'SQUAWKS',  n: window.SQUAWKS.filter(s => s.severity !== 'deferred').length },
          { k: 'mx',       l: 'MAINTENANCE', n: window.FLEET.filter(a => a.nextInspectionHrs < 10).length },
          { k: 'sessions', l: 'SESSIONS' },
          { k: 'editor',   l: 'AIRFRAME EDITOR' },
          { k: 'bind',     l: 'BINDINGS' },
          { k: 'log',      l: 'LOG' },
        ].map(t => (
          <button key={t.k} onClick={() => setTab(t.k)} style={{
            background: 'transparent',
            color: tab === t.k ? v4Pal.amber : v4Pal.dim,
            border: 'none',
            borderRight: '1px solid ' + v4Pal.border,
            borderBottom: tab === t.k ? '2px solid ' + v4Pal.amber : '2px solid transparent',
            padding: '10px 14px',
            fontFamily: v4Pal.mono, fontSize: 10, fontWeight: 600, letterSpacing: '0.12em',
            cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6,
            marginBottom: -1,
          }}>
            {t.l}
            {t.n != null && t.n > 0 && (
              <span style={{
                color: tab === t.k ? v4Pal.amber : v4Pal.faint,
                fontSize: 9,
              }}>[{t.n}]</span>
            )}
          </button>
        ))}
      </div>

      {/* Live strip — only visible when sim connected */}
      {simOk && liveAc && (
        <div style={{
          background: v4Pal.panel,
          borderBottom: '1px solid ' + v4Pal.border,
          padding: '10px 18px',
          display: 'flex', alignItems: 'center', gap: 22,
          fontFamily: v4Pal.mono, fontSize: 11,
          overflow: 'hidden',
        }}>
          <span style={{ color: v4Pal.green, fontWeight: 700, letterSpacing: '0.16em', display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <span style={{ width: 7, height: 7, borderRadius: '50%', background: v4Pal.green, animation: 'v4pulse 2s infinite' }}/>
            LIVE
          </span>
          <span style={{ color: v4Pal.text, fontWeight: 700 }}>{liveAc.tail}</span>
          <span style={{ color: v4Pal.dim }}>{liveAc.type}</span>
          <span style={{ color: v4Pal.faint }}>·</span>
          <span style={{ color: v4Pal.amber, letterSpacing: '0.12em', fontWeight: 600 }}>{ls.phase}</span>
          <span style={{ flex: 1 }}/>
          {[
            ['ALT', ls.altitude.toLocaleString() + ' ft'],
            ['IAS', ls.ias + ' kt'],
            ['GS',  ls.gs + ' kt'],
            ['HDG', String(ls.hdg).padStart(3,'0') + '°'],
            ['OAT', ls.oat + '°C'],
            ['N1',  ls.n1.map(v => v.toFixed(0)).join('/') + '%'],
            ['ITT', ls.itt.join('/') + '°C'],
            ['FUEL', ls.fuelKg + 'kg'],
            ['HOBBS+', '+' + (ls.currentHobbs - ls.hobbsStart).toFixed(1)],
          ].map(([k,v]) => (
            <span key={k} style={{ display: 'inline-flex', gap: 5, alignItems: 'baseline' }}>
              <span style={{ color: v4Pal.faint, fontSize: 9, letterSpacing: '0.1em' }}>{k}</span>
              <span style={{ color: v4Pal.text, fontWeight: 600 }}>{v}</span>
            </span>
          ))}
        </div>
      )}

      {/* Body — split: table + detail */}
      <div style={{ flex: 1, display: 'grid', gridTemplateColumns: '1fr 380px', minHeight: 0 }}>
        <main style={{ overflow: 'auto', minHeight: 0 }}>
          {tab === 'fleet' && (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ background: '#100e0a', position: 'sticky', top: 0 }}>
                  {['TAIL', 'TYPE', 'STATUS', 'HOBBS', 'CYCLES', 'SINCE MX', 'NEXT MX', 'OIL', 'TIRE', 'BRK', 'BAT', 'SQ', 'LAST FLT'].map(h => (
                    <th key={h} style={{
                      padding: '8px 14px', textAlign: 'left',
                      borderBottom: '1px solid ' + v4Pal.border,
                      fontFamily: v4Pal.mono, fontSize: 9, fontWeight: 600,
                      letterSpacing: '0.16em', color: v4Pal.faint,
                    }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {window.FLEET.map(a => {
                  const meta = window.STATUS_META[a.status];
                  const sel = a.tail === selectedTail;
                  return (
                    <tr key={a.id} onClick={() => setSelectedTail(a.tail)} style={{
                      cursor: 'pointer',
                      background: sel ? v4Pal.panel2 : a.live ? 'rgba(168,201,122,.04)' : 'transparent',
                    }}>
                      <td style={v4Cell({ color: v4Pal.text, fontWeight: 700 })}>
                        {a.live && <span style={{ color: v4Pal.green, marginRight: 6 }}>●</span>}
                        {a.tail}
                      </td>
                      <td style={v4Cell({ color: v4Pal.dim })}>{a.type}</td>
                      <td style={v4Cell({ color: meta.color, fontWeight: 600 })}>{meta.label}</td>
                      <td style={v4Cell()}>{a.hours.toFixed(1)}</td>
                      <td style={v4Cell({ color: v4Pal.dim })}>{a.cycles.toLocaleString()}</td>
                      <td style={v4Cell({ color: v4Pal.dim })}>{a.hobbsSinceMx.toFixed(1)}</td>
                      <td style={v4Cell({ color: a.nextInspectionHrs < 5 ? v4Pal.orange : v4Pal.text, fontWeight: a.nextInspectionHrs < 5 ? 700 : 400 })}>
                        {a.nextInspectionHrs.toFixed(1)}h
                      </td>
                      <BarCell value={a.consumables.oil}/>
                      <BarCell value={a.consumables.tires}/>
                      <BarCell value={a.consumables.brakes}/>
                      <BarCell value={a.consumables.battery}/>
                      <td style={v4Cell({ color: a.openSquawks ? v4Pal.orange : v4Pal.faint, fontWeight: 700 })}>
                        {a.openSquawks > 0 ? `!${a.openSquawks}` : '—'}
                        {a.deferred > 0 && <span style={{ color: v4Pal.dim, fontWeight: 400 }}> /{a.deferred}d</span>}
                      </td>
                      <td style={v4Cell({ color: v4Pal.dim })}>{a.lastFlight.date} · {a.lastFlight.dur}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}

          {tab === 'squawks' && (
            <div>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ background: '#100e0a' }}>
                    {['ID', 'OPENED', 'TAIL', 'COMPONENT', 'SUMMARY', 'SEV', 'MEL', 'AGE'].map(h => (
                      <th key={h} style={{ padding: '8px 14px', textAlign: 'left', borderBottom: '1px solid ' + v4Pal.border, fontFamily: v4Pal.mono, fontSize: 9, fontWeight: 600, letterSpacing: '0.16em', color: v4Pal.faint }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {window.SQUAWKS.map(s => {
                    const c = s.severity === 'grounding' ? v4Pal.red : s.severity === 'open' ? v4Pal.orange : v4Pal.dim;
                    return (
                      <tr key={s.id}>
                        <td style={v4Cell({ color: v4Pal.faint })}>{s.id}</td>
                        <td style={v4Cell({ color: v4Pal.dim })}>{s.opened}</td>
                        <td style={v4Cell({ color: v4Pal.text, fontWeight: 700 })}>{s.tail}</td>
                        <td style={v4Cell({ color: v4Pal.text })}>{s.component}</td>
                        <td style={v4Cell({ color: v4Pal.dim, whiteSpace: 'normal', maxWidth: 420 })}>{s.summary}</td>
                        <td style={v4Cell({ color: c, fontWeight: 700 })}>{s.severity.toUpperCase()}</td>
                        <td style={v4Cell({ color: s.melDeferrable ? v4Pal.dim : v4Pal.red })}>{s.melDeferrable ? 'YES' : 'NO'}</td>
                        <td style={v4Cell({ color: v4Pal.dim })}>—</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {tab === 'mx' && <MxTerminalView pal={v4Pal}/>}
          {tab === 'sessions' && <PlaceholderView text="No sessions logged yet — fly an airframe to populate the log." pal={v4Pal}/>}
          {tab === 'editor'   && <PlaceholderView text="Airframe editor — define component templates, MTBFs, wear curves, and L:Var bindings." pal={v4Pal}/>}
          {tab === 'bind'     && <PlaceholderView text="Bindings registry — map A/L/K vars per model_ref. 47 bindings across 4 model refs." pal={v4Pal}/>}
          {tab === 'log'      && <PlaceholderView text="Application log — Serilog rolling file, last 24 h." pal={v4Pal}/>}
        </main>

        {/* Detail */}
        <aside style={{
          background: v4Pal.panel,
          borderLeft: '1px solid ' + v4Pal.border,
          overflow: 'auto', minHeight: 0,
          fontFamily: v4Pal.mono, fontSize: 11,
        }}>
          {sel && <V4Detail a={sel} pal={v4Pal}/>}
        </aside>
      </div>

      {/* Status bar */}
      <div style={{
        height: 24, flex: '0 0 24px',
        background: '#0a0907',
        borderTop: '1px solid ' + v4Pal.border,
        display: 'flex', alignItems: 'center', gap: 18,
        padding: '0 14px',
        fontFamily: v4Pal.mono, fontSize: 10, color: v4Pal.faint,
        letterSpacing: '0.06em',
      }}>
        <span style={{ color: v4Pal.amber }}>READY</span>
        <span>·</span>
        <span>TICK 4 Hz</span>
        <span>·</span>
        <span>WEAR-ENGINE OK</span>
        <span>·</span>
        <span>FAILURE-ENGINE OK</span>
        <span style={{ flex: 1 }}/>
        <span>{window.FLEET.length} airframes loaded</span>
        <span>·</span>
        <span>{window.SQUAWKS.length} squawks</span>
      </div>

      <style>{`@keyframes v4pulse{0%,100%{opacity:1}50%{opacity:.35}}`}</style>
    </window.WindowsFrame>
  );
};

const BarCell = ({ value }) => {
  const c = value < 0.25 ? v4Pal.red : value < 0.5 ? v4Pal.orange : value < 0.75 ? v4Pal.amber : v4Pal.green;
  return (
    <td style={v4Cell({ minWidth: 80 })}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <div style={{ width: 36, height: 4, background: 'rgba(255,255,255,.06)' }}>
          <div style={{ width: value * 100 + '%', height: '100%', background: c }}/>
        </div>
        <span style={{ fontSize: 10, color: c, fontWeight: 600 }}>{Math.round(value * 100)}</span>
      </div>
    </td>
  );
};

const V4Detail = ({ a, pal }) => {
  const meta = window.STATUS_META[a.status];
  const sqs = window.SQUAWKS.filter(s => s.tail === a.tail);
  return (
    <div style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 14 }}>
      <div>
        <div style={{ fontSize: 9, letterSpacing: '0.16em', color: pal.faint }}>{'>>'} INSPECT</div>
        <div style={{ fontSize: 22, color: pal.text, fontWeight: 700, marginTop: 4 }}>{a.tail}</div>
        <div style={{ fontSize: 11, color: pal.dim }}>{a.model}</div>
        <div style={{ marginTop: 6, color: meta.color, fontWeight: 700, fontSize: 10, letterSpacing: '0.14em' }}>
          {meta.label}{a.live && <span style={{ color: pal.green, marginLeft: 8 }}>● IN SIM</span>}
        </div>
      </div>

      <V4Block pal={pal} title="LEDGER">
        <V4Row pal={pal} k="airframe_id"      v={a.id}/>
        <V4Row pal={pal} k="model_ref"        v={a.type}/>
        <V4Row pal={pal} k="total_hobbs"      v={a.hours.toFixed(1) + ' h'}/>
        <V4Row pal={pal} k="total_cycles"     v={a.cycles.toLocaleString()}/>
        <V4Row pal={pal} k="hobbs_since_mx"   v={a.hobbsSinceMx.toFixed(1) + ' h'}/>
        <V4Row pal={pal} k="next_inspection"  v={a.nextInspectionHrs.toFixed(1) + ' h'} alert={a.nextInspectionHrs < 5}/>
      </V4Block>

      <V4Block pal={pal} title="LAST_SESSION">
        <V4Row pal={pal} k="date"    v={a.lastFlight.date}/>
        <V4Row pal={pal} k="dur"     v={a.lastFlight.dur}/>
        <V4Row pal={pal} k="max_g"   v={a.lastFlight.maxG.toFixed(1)} alert={a.lastFlight.maxG > 2}/>
        <V4Row pal={pal} k="hard_landings" v={a.lastFlight.hardLandings} alert={a.lastFlight.hardLandings > 0}/>
        <V4Row pal={pal} k="overtemps"     v={a.lastFlight.overtemps} alert={a.lastFlight.overtemps > 0}/>
      </V4Block>

      <V4Block pal={pal} title={'SQUAWKS [' + sqs.length + ']'}>
        {sqs.length === 0 && <div style={{ color: pal.dim, fontStyle: 'italic' }}>// no open squawks</div>}
        {sqs.map(s => {
          const c = s.severity === 'grounding' ? pal.red : s.severity === 'open' ? pal.orange : pal.dim;
          return (
            <div key={s.id} style={{ display: 'flex', flexDirection: 'column', gap: 3, paddingBottom: 8, borderBottom: '1px dashed ' + pal.rule }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: pal.text, fontWeight: 700 }}>{s.component}</span>
                <span style={{ color: c, fontWeight: 700, fontSize: 9, letterSpacing: '0.14em' }}>{s.severity.toUpperCase()}</span>
              </div>
              <div style={{ color: pal.dim, fontSize: 10, lineHeight: 1.5, whiteSpace: 'normal' }}>{s.summary}</div>
              <div style={{ color: pal.faint, fontSize: 9, letterSpacing: '0.08em' }}>
                {s.id} · OPENED {s.opened} @ {s.hoursAtOpen.toFixed(1)}h
              </div>
            </div>
          );
        })}
      </V4Block>

      <V4Block pal={pal} title="ACTIONS">
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4 }}>
          {[
            ['+ NEW SQUAWK', pal.text],
            ['↳ DEFER (MEL)', pal.text],
            ['✓ SERVICE COMPONENT', pal.text],
            ['! OPEN AIRFRAME', pal.text],
            ['↻ RESET CYCLE', pal.text],
            ['⚠ GROUND', pal.red],
          ].map(([l, c]) => (
            <button key={l} style={{
              background: 'transparent',
              border: '1px solid ' + (c === pal.red ? 'rgba(194,84,80,.3)' : v4Pal.border),
              color: c, padding: '6px 8px',
              fontFamily: pal.mono, fontSize: 9, fontWeight: 600, letterSpacing: '0.08em',
              textAlign: 'left', cursor: 'pointer',
            }}>{l}</button>
          ))}
        </div>
      </V4Block>
    </div>
  );
};

const V4Block = ({ pal, title, children }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
    <div style={{ fontSize: 9, letterSpacing: '0.16em', color: pal.amber, fontWeight: 700 }}>
      [{title}]
    </div>
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>{children}</div>
  </div>
);

const V4Row = ({ pal, k, v, alert }) => (
  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11 }}>
    <span style={{ color: pal.dim }}>{k}</span>
    <span style={{ color: alert ? pal.orange : pal.text, fontWeight: 600 }}>{v}</span>
  </div>
);

const MxTerminalView = ({ pal }) => (
  <div style={{ padding: 14, fontFamily: pal.mono, fontSize: 11 }}>
    {window.FLEET.map(a => (
      <div key={a.id} style={{ padding: '12px 0', borderBottom: '1px solid ' + pal.rule, display: 'grid', gridTemplateColumns: '120px 1fr 140px', gap: 16, alignItems: 'center' }}>
        <div>
          <div style={{ color: pal.text, fontWeight: 700 }}>{a.tail}</div>
          <div style={{ color: pal.dim, fontSize: 10 }}>{a.type}</div>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 12 }}>
          {[['oil','OIL'],['tires','TIRE'],['brakes','BRK'],['battery','BAT'],['hyd','HYD']].map(([k,l]) => {
            const v = a.consumables[k];
            const c = v < 0.25 ? pal.red : v < 0.5 ? pal.orange : v < 0.75 ? pal.amber : pal.green;
            return (
              <div key={k} style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 9, letterSpacing: '0.1em' }}>
                  <span style={{ color: pal.faint }}>{l}</span>
                  <span style={{ color: c, fontWeight: 600 }}>{Math.round(v*100)}%</span>
                </div>
                <div style={{ height: 3, background: 'rgba(255,255,255,.06)' }}>
                  <div style={{ width: v*100 + '%', height: '100%', background: c }}/>
                </div>
              </div>
            );
          })}
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 9, color: pal.faint, letterSpacing: '0.12em' }}>NEXT INSPECTION</div>
          <div style={{ fontSize: 14, color: a.nextInspectionHrs < 5 ? pal.orange : pal.text, fontWeight: 700, marginTop: 2 }}>
            {a.nextInspectionHrs.toFixed(1)} h
          </div>
        </div>
      </div>
    ))}
  </div>
);

const PlaceholderView = ({ text, pal }) => (
  <div style={{ padding: 32, color: pal.dim, fontFamily: pal.mono, fontSize: 12, lineHeight: 1.7 }}>
    <div style={{ color: pal.amber, fontSize: 9, letterSpacing: '0.16em', marginBottom: 10, fontWeight: 700 }}>// PLACEHOLDER</div>
    {text}
  </div>
);

window.Variant4 = Variant4;
