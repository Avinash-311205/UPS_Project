import type { Page, Shipment, System } from '../types'
import type { UserRole } from '../auth/mockAuth'

export const initialSystems: System[] = [
  { name: 'Transportation (TMS)', description: 'Route planning & dispatch', status: 'Operational', latency: '42 ms', uptime: '99.98%', icon: 'truck' },
  { name: 'Warehouse (WMS)', description: 'Inventory & fulfillment', status: 'Operational', latency: '68 ms', uptime: '99.96%', icon: 'warehouse' },
  { name: 'EDI Gateway', description: 'Partner data exchange', status: 'Degraded', latency: '?', uptime: '98.42%', icon: 'arrows' },
  { name: 'Telematics', description: 'Fleet & vehicle signals', status: 'Operational', latency: '115 ms', uptime: '99.91%', icon: 'pin' },
  { name: 'Yard Management (YMS)', description: 'Dock & yard operations', status: 'Operational', latency: '54 ms', uptime: '99.99%', icon: 'layers' },
]

export const initialShipments: Shipment[] = [
  { id: 'SHP-28491', destination: 'Chicago, IL', customer: 'Northstar Retail', eta: 'Today, 2:40 PM', status: 'At risk', system: 'EDI Gateway' },
  { id: 'SHP-28487', destination: 'Milwaukee, WI', customer: 'Pioneer Foods', eta: 'Today, 3:15 PM', status: 'Continuity', system: 'TMS' },
  { id: 'SHP-28476', destination: 'Indianapolis, IN', customer: 'Westline Medical', eta: 'Today, 4:05 PM', status: 'At risk', system: 'EDI Gateway' },
  { id: 'SHP-28462', destination: 'Columbus, OH', customer: 'Evergreen Supply', eta: 'Today, 5:20 PM', status: 'On track', system: 'WMS' },
  { id: 'SHP-28451', destination: 'Detroit, MI', customer: 'Atlas Components', eta: 'Today, 6:10 PM', status: 'Continuity', system: 'TMS' },
]

export const currentDateLabel = new Intl.DateTimeFormat('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' }).format(new Date()).toUpperCase()
export const currentYear = new Date().getFullYear()
export const navItems: { label: Page; icon: string; group: string }[] = [
  { label: 'Overview', icon: 'grid', group: 'WORKSPACE' },
  { label: 'Shipments', icon: 'package', group: 'WORKSPACE' },
  { label: 'Systems', icon: 'server', group: 'RESILIENCE' },
  { label: 'Incidents', icon: 'shield', group: 'RESILIENCE' },
  { label: 'Continuity', icon: 'refresh', group: 'RESILIENCE' },
  { label: 'Recovery', icon: 'activity', group: 'RESILIENCE' },
]
export const pagesByRole: Record<UserRole, Page[]> = {
  'operations-manager': ['Overview', 'Shipments', 'Systems', 'Incidents', 'Continuity', 'Recovery'],
  'security-admin': ['Overview', 'Shipments', 'Systems', 'Incidents', 'Continuity', 'Recovery'],
  'hub-operator': ['Overview', 'Shipments', 'Incidents', 'Continuity'],
}
export const operatorShipmentIds = ['SHP-28487', 'SHP-28451']
export const workflowStorageKeys = {
  isolated: 'resiliencex-demo-isolated',
  approval: 'resiliencex-demo-approval',
  continuityActive: 'resiliencex-demo-continuity-active',
  permanentSystemsValidated: 'resiliencex-demo-systems-validated',
  reconciled: 'resiliencex-demo-reconciled',
  shipments: 'resiliencex-demo-shipments',
}

export function restoreShipments(): Shipment[] {
  const savedShipments = window.sessionStorage.getItem(workflowStorageKeys.shipments)
  if (!savedShipments) return initialShipments

  let parsedShipments: unknown
  try {
    parsedShipments = JSON.parse(savedShipments)
  } catch {
    throw new Error('Saved shipment updates could not be read. Clear this tab’s saved app data and try again.')
  }
  if (!Array.isArray(parsedShipments)) {
    throw new Error('Saved shipment updates could not be read. Clear this tab’s saved app data and try again.')
  }

  return initialShipments.map((shipment) => {
    const saved = parsedShipments.find((entry) => typeof entry === 'object' && entry !== null && 'id' in entry && entry.id === shipment.id)
    if (!saved) return shipment
    if (typeof saved !== 'object' || !('status' in saved) || !['On track', 'At risk', 'Continuity'].includes(String(saved.status))) {
      throw new Error('Saved shipment updates could not be read. Clear this tab’s saved app data and try again.')
    }

    return {
      ...shipment,
      status: saved.status as Shipment['status'],
      ...('lastUpdate' in saved && typeof saved.lastUpdate === 'string' ? { lastUpdate: saved.lastUpdate } : {}),
    }
  })
}
