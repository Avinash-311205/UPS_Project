import type { Modal, Page, Shipment, System } from '../types'
import type { UserRole } from '../auth/session'
import { roleCanPerformAction } from '../lib/workflow'
import Icon from '../components/Icon'
import MetricCard from '../components/MetricCard'
import PanelHeader from '../components/PanelHeader'
import SystemRow from '../components/SystemRow'
import ShipmentTable from '../components/ShipmentTable'
import ActivityItem from '../components/ActivityItem'

export default function OverviewPage({ role, systems, isolated, permanentSystemsValidated, reconciled, approval, continuityActive, shipments, onPage, onAction }: {
  role: UserRole
  systems: System[]
  isolated: boolean
  permanentSystemsValidated: boolean
  reconciled: boolean
  approval: 'pending' | 'approved' | 'declined'
  continuityActive: boolean
  shipments: Shipment[]
  onPage: (page: Page) => void
  onAction: (action: Modal) => void
}) {
  const atRisk = shipments.filter((shipment) => shipment.status === 'At risk').length
  return (
    <>
      <div className="incident-banner">
        <div className="incident-icon"><Icon name="warning" size={19} /></div>
        <div className="incident-copy"><div><span className="severity-tag">{reconciled ? 'INCIDENT RECOVERED' : permanentSystemsValidated ? 'RECOVERY IN PROGRESS' : 'ACTIVE INCIDENT'}</span><span className="incident-time">INC-2024-0842 · Detected 12 min ago</span></div><strong>{reconciled ? 'EDI Gateway restored and records reconciled' : permanentSystemsValidated ? 'EDI Gateway validated · reconciliation pending' : 'Suspicious activity detected on EDI Gateway'}</strong><p>{reconciled ? 'Trusted systems are online and continuity transactions have been reconciled.' : permanentSystemsValidated ? 'Permanent infrastructure passed validation. Review continuity records before closing the incident.' : 'Unusual outbound data patterns identified. 2 integrations may be impacted.'}</p></div>
        <div className="incident-actions">{!isolated && !permanentSystemsValidated && !reconciled ? roleCanPerformAction(role, 'isolate') ? <button className="button button-light" onClick={() => onAction('isolate')}>Isolate integration <Icon name="chevron" size={15} /></button> : <span className="contained-label">Security response in progress</span> : <span className="contained-label"><Icon name="check" size={14} />{reconciled ? 'Incident recovered' : permanentSystemsValidated ? 'Systems validated' : 'Integration isolated'}</span>}<button className="text-button incident-view" onClick={() => onPage('Incidents')}>View incident <Icon name="chevron" size={14} /></button></div>
      </div>

      <section className="metrics-grid" aria-label="Network summary">
        <MetricCard label="SYSTEMS MONITORED" value="05" icon="server" note={<><span className="metric-dot" />All systems connected</>} />
        <MetricCard label="SHIPMENTS AT RISK" value={String(atRisk).padStart(2, '0')} icon="package" note={<><span className="metric-warning" />Requires attention</>} />
        <MetricCard label="NETWORK UPTIME" value="99.97%" icon="activity" note={<><span className="metric-dot" />Last 30 days</>} />
        <MetricCard label="RECOVERY WINDOW" value="02:18" icon="clock" note={<><span className="metric-arrow"><Icon name="arrowUp" size={12} /></span>Within target window</>} />
      </section>

      <div className="dashboard-grid">
        <div className="dashboard-main">
          <section className="panel system-panel">
            <PanelHeader title="System health" subtitle="Live status across your logistics network" action="View all systems" onAction={() => onPage('Systems')} />
            <div className="system-list">
              {systems.map((system) => <SystemRow key={system.name} system={system} />)}
            </div>
            <div className="system-footnote"><span><span className="live-dot" />Live monitoring active</span><span>Last checked just now <span className="sync-icon">↻</span></span></div>
          </section>

          <section className="panel shipment-panel">
            <PanelHeader title="Impacted shipments" subtitle={`${atRisk} shipments need your attention`} action="View all shipments" onAction={() => onPage('Shipments')} />
            <ShipmentTable shipments={shipments.slice(0, 3)} />
          </section>
        </div>
        <aside className="dashboard-side">
          <section className="panel response-panel">
            <div className="section-title-row"><div><div className="card-kicker">RECOMMENDED ACTION</div><h2>Continuity plan</h2></div><span className={`plan-badge ${approval === 'approved' ? 'approved' : approval === 'declined' ? 'declined' : ''}`}>{approval === 'pending' ? 'PENDING' : approval.toUpperCase()}</span></div>
            <p className="plan-description">{approval === 'declined' ? 'This plan was declined. Review the incident and submit a new approval when ready.' : 'Keep eligible shipments moving with a secure backup workspace while EDI Gateway is investigated.'}</p>
            <div className="plan-impact"><div><span className="impact-number">12</span><span>shipments covered</span></div><div><span className="impact-number">3</span><span>facilities supported</span></div></div>
            <div className="plan-checklist"><div><span className="step-check"><Icon name="check" size={12} /></span>Isolate affected integrations</div><div><span className={`step-check ${approval === 'pending' ? 'step-wait' : ''}`}><Icon name={approval === 'pending' ? 'clock' : 'check'} size={12} /></span>Manager approval required</div><div><span className={`step-check ${continuityActive ? '' : 'step-wait'}`}><Icon name={continuityActive ? 'check' : 'clock'} size={12} /></span>Activate secure workspace</div></div>
            {approval === 'pending' && roleCanPerformAction(role, 'approve') ? <div className="plan-buttons"><button className="button button-primary" onClick={() => onAction('approve')}>Review & approve</button><button className="button button-quiet" onClick={() => onAction('decline')}>Decline</button></div> : approval === 'pending' ? <div className="approval-note"><Icon name="clock" size={15} />Awaiting explicit Operations Manager approval.</div> : <button className="button button-primary full-button" onClick={() => onPage(continuityActive ? 'Continuity' : 'Incidents')}>{continuityActive ? 'Open continuity workspace' : 'Review incident'}<Icon name="chevron" size={15} /></button>}
          </section>
          <section className="panel activity-panel">
            <PanelHeader title="Recent activity" action="View incident" onAction={() => onPage('Incidents')} />
            <div className="activity-list">
              <ActivityItem icon="alert" tone="orange" title="Suspicious EDI traffic detected" detail="Intrusion Detection System · 12 min ago" />
              <ActivityItem icon="activity" tone="green" title="Dependency check completed" detail="5 systems verified · 28 min ago" />
              <ActivityItem icon="check" tone="brown" title="Backup environment synced" detail="Continuity database · 46 min ago" />
            </div>
          </section>
        </aside>
      </div>
    </>
  )
}
