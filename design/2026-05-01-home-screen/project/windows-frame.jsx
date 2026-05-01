// Minimal Windows 11-style window chrome.
// Pass {title, accent, children, width, height} — tiny, doesn't try to be
// Fluent UI, just enough chrome to read as a desktop app.

const WindowsFrame = ({ title = 'msfs-failures', subtitle, width = 1440, height = 900, accent = '#d4a24c', children, bg = '#1a1815' }) => {
  return (
    <div style={{
      width, height,
      borderRadius: 8,
      overflow: 'hidden',
      background: bg,
      boxShadow: '0 32px 80px rgba(0,0,0,.45), 0 0 0 1px rgba(255,255,255,.06)',
      display: 'flex', flexDirection: 'column',
      fontFamily: '"Inter", "Segoe UI Variable", "Segoe UI", system-ui, sans-serif',
      color: '#e8e3da',
      position: 'relative',
    }}>
      {/* Title bar */}
      <div style={{
        height: 36,
        flex: '0 0 36px',
        background: '#0f0d0b',
        borderBottom: '1px solid rgba(255,255,255,.05)',
        display: 'flex', alignItems: 'center',
        paddingLeft: 12,
        gap: 10,
        userSelect: 'none',
        fontSize: 12,
      }}>
        {/* App icon (square chip) */}
        <div style={{
          width: 16, height: 16, borderRadius: 3,
          background: `linear-gradient(135deg, ${accent} 0%, #8b5a3a 100%)`,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: '"JetBrains Mono", monospace', fontSize: 9, fontWeight: 700,
          color: '#0f0d0b',
        }}>F</div>
        <span style={{ color: '#a89e8e', fontWeight: 500 }}>{title}</span>
        {subtitle && (
          <>
            <span style={{ color: '#534b40' }}>—</span>
            <span style={{ color: '#7d7466', fontFamily: '"JetBrains Mono", monospace', fontSize: 11 }}>
              {subtitle}
            </span>
          </>
        )}

        <div style={{ flex: 1 }} />

        {/* Window controls */}
        <div style={{ display: 'flex', height: '100%' }}>
          {['min', 'max', 'close'].map((kind) => (
            <div key={kind} style={{
              width: 46, height: '100%',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              cursor: 'default',
              color: '#7d7466',
              fontSize: 10,
            }}>
              {kind === 'min' && <svg width="10" height="10"><line x1="0" y1="5" x2="10" y2="5" stroke="currentColor" strokeWidth="1"/></svg>}
              {kind === 'max' && <svg width="10" height="10"><rect x="0.5" y="0.5" width="9" height="9" fill="none" stroke="currentColor" strokeWidth="1"/></svg>}
              {kind === 'close' && <svg width="10" height="10"><line x1="0" y1="0" x2="10" y2="10" stroke="currentColor" strokeWidth="1"/><line x1="10" y1="0" x2="0" y2="10" stroke="currentColor" strokeWidth="1"/></svg>}
            </div>
          ))}
        </div>
      </div>

      <div style={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        {children}
      </div>
    </div>
  );
};

window.WindowsFrame = WindowsFrame;
