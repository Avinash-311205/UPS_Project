import type { AuthUser } from './session'
import { IncidentStatus } from '../types'

export const RoleCodes = {
  Admin: 'ADMIN',
  HubOperator: 'HUB_OPERATOR',
  SecurityManager: 'SECURITY_MANAGER',
  Unassigned: 'UNASSIGNED',
} as const
export type RoleCode = (typeof RoleCodes)[keyof typeof RoleCodes]

export const roleCodeDisplayNames: Record<string, string> = {
  ADMIN: 'Admin',
  HUB_OPERATOR: 'Hub Operator',
  SECURITY_MANAGER: 'Security Manager',
  UNASSIGNED: 'Unassigned',
}

/**
 * Evaluates whether the authenticated user has permission to manage systems
 * (Create, Update, Deactivate).
 * Root and Admin have full management; Hub Operator and Security Manager are read-only.
 */
export function canManageSystems(user: AuthUser): boolean {
  if (user.accountType === 'Root') return true
  return user.roleCode === RoleCodes.Admin
}

/**
 * Evaluates whether the authenticated user has permission to manage dependencies
 * (Create, Update, Delete).
 * Root and Admin have full management; Hub Operator and Security Manager are read-only.
 */
export function canManageDependencies(user: AuthUser): boolean {
  if (user.accountType === 'Root') return true
  return user.roleCode === RoleCodes.Admin
}

/**
 * Evaluates whether the authenticated user has permission to manage business operations
 * (Create, Update, Deactivate, Assign Systems).
 * Root and Admin have full management; Hub Operator and Security Manager are read-only.
 */
export function canManageOperations(user: AuthUser): boolean {
  if (user.accountType === 'Root') return true
  return user.roleCode === RoleCodes.Admin
}

/**
 * Evaluates whether the authenticated user has permission to report new disruption incidents.
 * Root, Admin, Hub Operator, and Security Manager can report; Unassigned cannot.
 */
export function canReportIncidents(user: AuthUser): boolean {
  if (user.accountType === 'Root') return true
  return (
    user.roleCode === RoleCodes.Admin ||
    user.roleCode === RoleCodes.HubOperator ||
    user.roleCode === RoleCodes.SecurityManager
  )
}

/**
 * Evaluates whether the authenticated user can edit incident details.
 * Root, Admin, Hub Operator, and Security Manager can edit; Unassigned cannot.
 */
export function canEditIncidents(user: AuthUser): boolean {
  return canReportIncidents(user)
}

/**
 * Evaluates whether the authenticated user can transition incident lifecycle status.
 */
export function canTransitionIncidentStatus(user: AuthUser): boolean {
  return canReportIncidents(user)
}

/**
 * Determines which target statuses a user is authorized to transition an incident to.
 * - Hub Operator and Security Manager CANNOT close incidents and CANNOT reopen resolved incidents.
 * - Root and Admin can transition to any valid lifecycle status.
 */
export function getAllowedTargetStatuses(
  user: AuthUser,
  currentStatus: IncidentStatus
): IncidentStatus[] {
  const isElevated = user.accountType === 'Root' || user.roleCode === RoleCodes.Admin
  const isFrontline =
    user.roleCode === RoleCodes.HubOperator || user.roleCode === RoleCodes.SecurityManager

  if (!isElevated && !isFrontline) return []

  const allStatuses = [
    IncidentStatus.Open,
    IncidentStatus.Investigating,
    IncidentStatus.Mitigating,
    IncidentStatus.Resolved,
    IncidentStatus.Closed,
  ]

  if (isElevated) {
    return allStatuses.filter((s) => s !== currentStatus)
  }

  // Frontline: Hub Operator / Security Manager
  return allStatuses.filter((s) => {
    if (s === currentStatus) return false
    // Cannot transition to Closed
    if (s === IncidentStatus.Closed) return false
    // Cannot transition to Open (reopen) from Resolved
    if (currentStatus === IncidentStatus.Resolved && s === IncidentStatus.Open) return false
    return true
  })
}

/**
 * Evaluates whether the authenticated user can add investigation notes.
 * Root, Admin, and Security Manager can add notes; Hub Operator and Unassigned cannot.
 */
export function canAddIncidentNotes(user: AuthUser): boolean {
  if (user.accountType === 'Root') return true
  return user.roleCode === RoleCodes.Admin || user.roleCode === RoleCodes.SecurityManager
}

/**
 * Evaluates whether the user can manage IAM workforce. Strictly Root only.
 */
export function canManageIam(user: AuthUser): boolean {
  return user.accountType === 'Root'
}
