import Icon from './Icon'

export default function PanelHeader({ title, subtitle, action, onAction }: { title: string; subtitle?: string; action?: string; onAction?: () => void }) {
  return <div className="panel-header"><div><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>{action && <button className="text-button panel-link" onClick={onAction}>{action}<Icon name="chevron" size={14} /></button>}</div>
}
