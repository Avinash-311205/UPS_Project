import type { System } from '../types'
import Icon from './Icon'
import StatusPill from './StatusPill'

export default function SystemRow({ system }: { system: System }) {
  return <div className="system-row"><div className="system-icon"><Icon name={system.icon} size={17} /></div><div className="system-name"><strong>{system.name}</strong><span>{system.description}</span></div><StatusPill status={system.status} /><div className="system-metric"><span>LATENCY</span><b>{system.latency}</b></div><div className="system-metric uptime-metric"><span>UPTIME</span><b>{system.uptime}</b></div><span className={`health-spark ${system.status.toLowerCase()}`} aria-label={`${system.name} health trend`}><i /><i /><i /><i /><i /><i /><i /></span></div>
}
