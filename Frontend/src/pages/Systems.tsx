import type { System } from '../types'
import Icon from '../components/Icon'
import StatusPill from '../components/StatusPill'
import PanelHeader from '../components/PanelHeader'

export default function SystemsPage({ systems, canRestore, onRestore }: { systems: System[]; canRestore: boolean; onRestore: () => void }) {
  return <div className="page-stack">
    <section className="panel systems-page-panel"><PanelHeader title="System health" subtitle="Health checks and dependency status for all five connected systems" /><div className="systems-table">
      <div className="systems-table-head"><span>SYSTEM</span><span>STATUS</span><span>LATENCY</span><span>UPTIME (30D)</span><span>HEALTH</span><span>ACTION</span></div>
      {systems.map((system) => <div className="systems-table-row" key={system.name}><div className="systems-table-name"><span className="system-icon"><Icon name={system.icon} size={18} /></span><div><strong>{system.name}</strong><small>{system.description}</small></div></div><StatusPill status={system.status} /><b className="latency-value">{system.latency}</b><b className="uptime-value">{system.uptime}</b><span className={`health-spark ${system.status.toLowerCase()}`}><i /><i /><i /><i /><i /><i /><i /></span>{canRestore && system.name === 'EDI Gateway' && system.status !== 'Operational' ? <button className="text-button table-action" onClick={onRestore}>Validate & restore</button> : <span className="action-muted">—</span>}</div>)}
    </div><div className="panel-footnote"><span className="live-dot" />System dependencies are monitored continuously. Anomaly detection is active.</div></section>
    <div className="dependency-panel panel"><div><div className="card-kicker">DEPENDENCY MAP</div><h2>Connected operations</h2><p>Isolation protects downstream systems while preserving essential shipment workflows.</p></div><div className="dependency-flow"><span className="dependency-node">IDS</span><span className="dependency-line" /><span className="dependency-node affected">EDI</span><span className="dependency-line" /><span className="dependency-node">TMS</span><span className="dependency-line" /><span className="dependency-node">WMS</span><span className="dependency-line" /><span className="dependency-node">YMS</span><span className="dependency-node">Telematics</span></div></div>
  </div>
}
