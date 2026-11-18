import type { ReactNode } from 'react'
import Icon from './Icon'

export default function MetricCard({ label, value, icon, note }: { label: string; value: string; icon: string; note: ReactNode }) {
  return <article className="metric-card"><div className="metric-top"><span>{label}</span><span className="metric-icon"><Icon name={icon} size={17} /></span></div><strong className="metric-value">{value}</strong><div className="metric-note">{note}</div></article>
}
