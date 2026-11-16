import type { Modal } from '../types'
import Icon from './Icon'

export default function ActionModal({ modal, onConfirm, onClose }: { modal: Exclude<Modal, null>; onConfirm: () => void; onClose: () => void }) {
  const content: Record<Exclude<Modal, null>, { eyebrow: string; title: string; description: string; confirm: string; icon: string }> = {
    isolate: { eyebrow: 'CONTAINMENT ACTION', title: 'Isolate EDI Gateway?', description: 'This will temporarily disable EDI integrations and contain connected activity. Shipment visibility may be reduced until continuity procedures are active.', confirm: 'Confirm isolation', icon: 'shield' },
    approve: { eyebrow: 'MANAGER APPROVAL', title: 'Approve continuity plan?', description: 'Approval activates a secure temporary workspace and separate continuity database for eligible shipment, dispatch, vehicle, and warehouse updates.', confirm: 'Approve & activate', icon: 'check' },
    decline: { eyebrow: 'MANAGER DECISION', title: 'Decline continuity plan?', description: 'The backup workspace will not be activated. The response team will be notified that the plan needs further review.', confirm: 'Decline plan', icon: 'close' },
    restore: { eyebrow: 'SYSTEM RECOVERY', title: 'Restore EDI Gateway?', description: 'Confirm that the recovered integration has passed validation before reconnecting it to permanent operations.', confirm: 'Confirm validated restore', icon: 'refresh' },
    reconcile: { eyebrow: 'DATA RECONCILIATION', title: 'Reconcile continuity records?', description: 'Temporary operational records will be checked against the permanent database. Duplicate and inconsistent transactions will be held for review.', confirm: 'Start reconciliation', icon: 'arrows' },
  }
  const item = content[modal]
  return <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}><section className="action-modal" role="dialog" aria-modal="true" aria-labelledby="modal-title"><button className="modal-close icon-button" type="button" aria-label="Close dialog" onClick={onClose}><Icon name="close" size={18} /></button><div className={`modal-icon ${modal === 'decline' ? 'modal-icon-warning' : ''}`}><Icon name={item.icon} size={21} /></div><span className="card-kicker">{item.eyebrow}</span><h2 id="modal-title">{item.title}</h2><p>{item.description}</p><div className="modal-actions"><button className="button button-outline" onClick={onClose}>Cancel</button><button className={`button ${modal === 'decline' ? 'button-danger' : 'button-primary'}`} onClick={onConfirm}>{item.confirm}</button></div></section></div>
}
