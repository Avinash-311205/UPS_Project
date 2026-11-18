export type Page = 'Overview' | 'Systems' | 'Incidents' | 'Shipments' | 'Continuity' | 'Recovery'
export type SystemStatus = 'Operational' | 'Degraded' | 'Isolated' | 'Recovering'
export type ShipmentStatus = 'On track' | 'At risk' | 'Continuity'
export type Modal = 'isolate' | 'approve' | 'decline' | 'restore' | 'reconcile' | null
export type System = { name: string; description: string; status: SystemStatus; latency: string; uptime: string; icon: string }
export type Shipment = { id: string; destination: string; customer: string; eta: string; status: ShipmentStatus; system: string; lastUpdate?: string }
