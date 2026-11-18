import { initialSystems, workflowStorageKeys } from '../data/mockData'
import type { System, Modal } from '../types'
import type { UserRole } from '../auth/session'

export function restoreApproval(): 'pending' | 'approved' | 'declined' {
  const savedApproval = window.sessionStorage.getItem(workflowStorageKeys.approval)
  return savedApproval === 'approved' || savedApproval === 'declined' ? savedApproval : 'pending'
}

export function restoreSystems(): System[] {
  const isValidated = window.sessionStorage.getItem(workflowStorageKeys.permanentSystemsValidated) === 'true'
  const isIsolated = window.sessionStorage.getItem(workflowStorageKeys.isolated) === 'true'
  return initialSystems.map((system) => system.name === 'EDI Gateway'
    ? { ...system, status: isValidated ? 'Operational' : isIsolated ? 'Isolated' : system.status }
    : system)
}

export function roleCanPerformAction(role: UserRole, action: Modal): boolean {
  if (!action) return false
  if (action === 'approve' || action === 'decline') return role === 'operations-manager'
  if (action === 'isolate' || action === 'restore' || action === 'reconcile') return role === 'security-admin'
  return false
}
