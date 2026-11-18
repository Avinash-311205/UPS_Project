import Icon from './Icon'

export default function ActivityItem({ icon, tone, title, detail }: { icon: string; tone: string; title: string; detail: string }) {
  return <div className="activity-item"><span className={`activity-icon ${tone}`}><Icon name={icon} size={15} /></span><div><strong>{title}</strong><small>{detail}</small></div><span className="activity-chevron"><Icon name="chevron" size={14} /></span></div>
}
