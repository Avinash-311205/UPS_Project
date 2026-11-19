const systems = [
  {
    acronym: 'TMS',
    name: 'Transportation Management',
    description: 'Route planning, carrier management, and dispatch',
    status: 'Operational',
    statusClass: 'operational',
    uptime: '99.98%',
    latency: '42 ms',
  },
  {
    acronym: 'WMS',
    name: 'Warehouse Management',
    description: 'Inventory control and fulfillment',
    status: 'Operational',
    statusClass: 'operational',
    uptime: '99.96%',
    latency: '68 ms',
  },
  {
    acronym: 'YMS',
    name: 'Yard Management',
    description: 'Dock scheduling and yard movements',
    status: 'Operational',
    statusClass: 'operational',
    uptime: '99.99%',
    latency: '54 ms',
  },
  {
    acronym: 'EDI',
    name: 'EDI Gateway',
    description: 'Partner data exchange and transaction handling',
    status: 'Degraded',
    statusClass: 'degraded',
    uptime: '98.42%',
    latency: 'Reviewing',
  },
] as const

export default function StaticHome({ displayName }: { displayName: string }) {
  return (
    <main className="static-home">
      <header className="static-header">
        <div className="auth-wordmark static-wordmark" aria-label="ResilienceX">
          <span className="auth-wordmark-mark">R<span>X</span></span>
          <span>resilience<span className="auth-wordmark-accent">X</span></span>
        </div>
        <div className="static-user" aria-label="Signed-in user">
          <span>{displayName.slice(0, 1).toUpperCase()}</span>
          <div>
            <strong>{displayName}</strong>
            <small>Operations workspace</small>
          </div>
        </div>
      </header>

      <section className="static-content" aria-labelledby="home-heading">
        <div className="static-heading">
          <div>
            <p className="static-eyebrow">Network overview</p>
            <h1 id="home-heading">Good morning, {displayName}</h1>
            <p>Current status across your logistics systems.</p>
          </div>
          <div className="network-summary">
            <span className="live-dot" />
            3 operational
            <span className="summary-divider" />
            1 needs attention
          </div>
        </div>

        <div className="system-grid" aria-label="Logistics system status">
          {systems.map((system) => (
            <article className="system-card" key={system.acronym} tabIndex={0}>
              <div className="system-card-top">
                <span className="system-acronym">{system.acronym}</span>
                <span className={`system-status ${system.statusClass}`}>
                  <span />
                  {system.status}
                </span>
              </div>
              <div className="system-card-body">
                <h2>{system.name}</h2>
                <p>{system.description}</p>
              </div>
              <div className="system-card-details">
                <div>
                  <span>Availability</span>
                  <strong>{system.uptime}</strong>
                </div>
                <div>
                  <span>Response</span>
                  <strong>{system.latency}</strong>
                </div>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  )
}
