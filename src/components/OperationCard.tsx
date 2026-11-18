import Icon from './Icon'

export default function OperationCard({ icon, title, detail, count }: { icon: string; title: string; detail: string; count: string }) {
  return <article className="operation-card"><span className="operation-icon"><Icon name={icon} size={18} /></span><h3>{title}</h3><p>{detail}</p><span className="operation-count">{count}</span></article>
}
