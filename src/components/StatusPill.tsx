import type { SystemStatus, ShipmentStatus } from '../types'

export default function StatusPill({ status }: { status: SystemStatus | ShipmentStatus }) {
  const className = status.toLowerCase().replaceAll(' ', '-')
  return <span className={`status-pill ${className}`}><span className="status-dot" />{status}</span>
}
